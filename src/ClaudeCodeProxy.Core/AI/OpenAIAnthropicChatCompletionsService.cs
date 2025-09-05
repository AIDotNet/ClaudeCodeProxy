using System.Runtime.CompilerServices;
using System.Text.Json;
using ClaudeCodeProxy.Abstraction;
using ClaudeCodeProxy.Abstraction.Anthropic;
using ClaudeCodeProxy.Domain;
using Microsoft.Extensions.Logging;
using Thor.Abstractions;
using Thor.Abstractions.Chats;
using Thor.Abstractions.Chats.Dtos;

namespace ClaudeCodeProxy.Core.AI;

/// <summary>
///     OpenAI到Claude适配器服务
///     将Claude格式的请求转换为OpenAI格式，然后将OpenAI的响应转换为Claude格式
/// </summary>
public class OpenAIAnthropicChatCompletionsService : AnthropicBase
{
    private readonly ILogger<OpenAIAnthropicChatCompletionsService> _logger;
    private readonly IThorChatCompletionsService _openAIChatService;

    public OpenAIAnthropicChatCompletionsService(
        IThorChatCompletionsService openAIChatService,
        ILogger<OpenAIAnthropicChatCompletionsService> logger)
    {
        _openAIChatService = openAIChatService;
        _logger = logger;
    }

    /// <summary>
    ///     非流式对话补全
    /// </summary>
    public async Task<AnthropicChatCompletionDto> ChatCompletionsAsync(AnthropicInput input,
        Dictionary<string, string> headers,
        ProxyConfig? config,
        ThorPlatformOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 转换请求格式：Claude -> OpenAI
            var openAIRequest = ConvertAnthropicToOpenAI(input);

            // 调用OpenAI服务
            var openAIResponse =
                await _openAIChatService.ChatCompletionsAsync(openAIRequest, headers, config, options,
                    cancellationToken);

            // 转换响应格式：OpenAI -> Claude
            var claudeResponse = ConvertOpenAIToClaude(openAIResponse, input);

            return claudeResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI到Claude适配器异常");
            throw;
        }
    }

    /// <summary>
    ///     流式对话补全
    /// </summary>
    public async IAsyncEnumerable<(string?, string?, AnthropicStreamDto?)> StreamChatCompletionsAsync(AnthropicInput input,
        Dictionary<string, string> headers,
        ProxyConfig? config,
        ThorPlatformOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var openAiRequest = ConvertAnthropicToOpenAI(input);
        openAiRequest.Stream = true;

        var messageId = Guid.NewGuid().ToString();
        var hasStarted = false;
        var hasTextContentBlockStarted = false;
        var hasThinkingContentBlockStarted = false;
        var toolBlocksStarted = new Dictionary<int, bool>(); // 使用索引而不是ID
        var toolCallIds = new Dictionary<int, string>(); // 存储每个索引对应的ID
        var toolCallIndexToBlockIndex = new Dictionary<int, int>(); // 工具调用索引到块索引的映射
        var accumulatedUsage = new AnthropicCompletionDtoUsage();
        var isFinished = false;
        var currentContentBlockType = ""; // 跟踪当前内容块类型
        var currentBlockIndex = 0; // 跟踪当前块索引
        var lastContentBlockType = ""; // 跟踪最后一个内容块类型，用于确定停止原因

        await foreach (var openAiResponse in _openAIChatService.StreamChatCompletionsAsync(openAiRequest, headers,
                           config, options, cancellationToken))
        {
            // 发送message_start事件
            if (!hasStarted && openAiResponse.Choices?.Count > 0 &&
                !openAiResponse.Choices.Any(x => x.Delta.ToolCalls?.Count > 0))
            {
                hasStarted = true;
                var messageStartEvent = CreateMessageStartEvent(messageId, input.Model);
                yield return ("message_start",
                    JsonSerializer.Serialize(messageStartEvent, ThorJsonSerializer.DefaultOptions), messageStartEvent);
            }

            // 更新使用情况统计
            if (openAiResponse.Usage != null)
            {
                // 使用最新的token计数（OpenAI通常在最后的响应中提供完整的统计）
                if (openAiResponse.Usage.PromptTokens.HasValue)
                    accumulatedUsage.InputTokens = openAiResponse.Usage.PromptTokens.Value;

                if (openAiResponse.Usage.CompletionTokens.HasValue)
                    accumulatedUsage.OutputTokens = (int)openAiResponse.Usage.CompletionTokens.Value;

                if (openAiResponse.Usage.PromptTokensDetails?.CachedTokens.HasValue == true)
                    accumulatedUsage.CacheReadInputTokens =
                        openAiResponse.Usage.PromptTokensDetails.CachedTokens.Value;

                // 记录调试信息
                _logger.LogDebug("OpenAI Usage更新: Input={InputTokens}, Output={OutputTokens}, CacheRead={CacheRead}",
                    accumulatedUsage.InputTokens, accumulatedUsage.OutputTokens,
                    accumulatedUsage.CacheReadInputTokens);
            }

            if (openAiResponse.Choices is { Count: > 0 })
            {
                var choice = openAiResponse.Choices.First();

                // 处理内容
                if (!string.IsNullOrEmpty(choice.Delta?.Content))
                {
                    // 如果当前有其他类型的内容块在运行，先结束它们
                    if (currentContentBlockType != "text" && !string.IsNullOrEmpty(currentContentBlockType))
                    {
                        var stopEvent = CreateContentBlockStopEvent();
                        stopEvent.Index = currentBlockIndex;
                        yield return ("content_block_stop",
                            JsonSerializer.Serialize(stopEvent, ThorJsonSerializer.DefaultOptions), stopEvent);
                        currentBlockIndex++; // 切换内容块时增加索引
                        currentContentBlockType = "";
                    }

                    // 发送content_block_start事件（仅第一次）
                    if (!hasTextContentBlockStarted || currentContentBlockType != "text")
                    {
                        hasTextContentBlockStarted = true;
                        currentContentBlockType = "text";
                        lastContentBlockType = "text";
                        var contentBlockStartEvent = CreateContentBlockStartEvent();
                        contentBlockStartEvent.Index = currentBlockIndex;
                        yield return ("content_block_start",
                            JsonSerializer.Serialize(contentBlockStartEvent, ThorJsonSerializer.DefaultOptions),
                            contentBlockStartEvent);
                    }

                    // 发送content_block_delta事件
                    var contentDeltaEvent = CreateContentBlockDeltaEvent(choice.Delta.Content);
                    contentDeltaEvent.Index = currentBlockIndex;
                    yield return ("content_block_delta",
                        JsonSerializer.Serialize(contentDeltaEvent, ThorJsonSerializer.DefaultOptions),
                        contentDeltaEvent);
                }

                // 处理工具调用
                if (choice.Delta?.ToolCalls is { Count: > 0 })
                    foreach (var toolCall in choice.Delta.ToolCalls)
                    {
                        var toolCallIndex = toolCall.Index; // 使用索引来标识工具调用

                        // 发送tool_use content_block_start事件
                        if (toolBlocksStarted.TryAdd(toolCallIndex, true))
                        {
                            // 如果当前有文本或thinking内容块在运行，先结束它们
                            if (currentContentBlockType == "text" || currentContentBlockType == "thinking")
                            {
                                var stopEvent = CreateContentBlockStopEvent();
                                stopEvent.Index = currentBlockIndex;
                                yield return ("content_block_stop",
                                    JsonSerializer.Serialize(stopEvent, ThorJsonSerializer.DefaultOptions), stopEvent);
                                currentBlockIndex++; // 增加块索引
                            }
                            // 如果当前有其他工具调用在运行，也需要结束它们
                            else if (currentContentBlockType == "tool_use")
                            {
                                var stopEvent = CreateContentBlockStopEvent();
                                stopEvent.Index = currentBlockIndex;
                                yield return ("content_block_stop",
                                    JsonSerializer.Serialize(stopEvent, ThorJsonSerializer.DefaultOptions), stopEvent);
                                currentBlockIndex++; // 增加块索引
                            }

                            currentContentBlockType = "tool_use";
                            lastContentBlockType = "tool_use";

                            // 为此工具调用分配一个新的块索引
                            toolCallIndexToBlockIndex[toolCallIndex] = currentBlockIndex;

                            // 保存工具调用的ID（如果有的话）
                            if (!string.IsNullOrEmpty(toolCall.Id))
                                toolCallIds[toolCallIndex] = toolCall.Id;
                            else if (!toolCallIds.ContainsKey(toolCallIndex))
                                // 如果没有ID且之前也没有保存过，生成一个新的ID
                                toolCallIds[toolCallIndex] = Guid.NewGuid().ToString();

                            var toolBlockStartEvent = CreateToolBlockStartEvent(
                                toolCallIds[toolCallIndex],
                                toolCall.Function?.Name);
                            toolBlockStartEvent.Index = currentBlockIndex;
                            yield return ("content_block_start",
                                JsonSerializer.Serialize(toolBlockStartEvent, ThorJsonSerializer.DefaultOptions),
                                toolBlockStartEvent);
                        }

                        // 如果有增量的参数，发送content_block_delta事件
                        if (!string.IsNullOrEmpty(toolCall.Function?.Arguments))
                        {
                            var toolDeltaEvent = CreateToolBlockDeltaEvent(toolCall.Function.Arguments);
                            // 使用该工具调用对应的块索引
                            toolDeltaEvent.Index = toolCallIndexToBlockIndex[toolCallIndex];
                            yield return ("content_block_delta",
                                JsonSerializer.Serialize(toolDeltaEvent, ThorJsonSerializer.DefaultOptions),
                                toolDeltaEvent);
                        }
                    }

                // 处理推理内容
                if (!string.IsNullOrEmpty(choice.Delta?.ReasoningContent))
                {
                    // 如果当前有其他类型的内容块在运行，先结束它们
                    if (currentContentBlockType != "thinking" && !string.IsNullOrEmpty(currentContentBlockType))
                    {
                        var stopEvent = CreateContentBlockStopEvent();
                        stopEvent.Index = currentBlockIndex;
                        yield return ("content_block_stop",
                            JsonSerializer.Serialize(stopEvent, ThorJsonSerializer.DefaultOptions), stopEvent);
                        currentBlockIndex++; // 增加块索引
                        currentContentBlockType = "";
                    }

                    // 对于推理内容，也需要发送对应的事件
                    if (!hasThinkingContentBlockStarted || currentContentBlockType != "thinking")
                    {
                        hasThinkingContentBlockStarted = true;
                        currentContentBlockType = "thinking";
                        lastContentBlockType = "thinking";
                        var thinkingBlockStartEvent = CreateThinkingBlockStartEvent();
                        thinkingBlockStartEvent.Index = currentBlockIndex;
                        yield return ("content_block_start",
                            JsonSerializer.Serialize(thinkingBlockStartEvent, ThorJsonSerializer.DefaultOptions),
                            thinkingBlockStartEvent);
                    }

                    var thinkingDeltaEvent = CreateThinkingBlockDeltaEvent(choice.Delta.ReasoningContent);
                    thinkingDeltaEvent.Index = currentBlockIndex;
                    yield return ("content_block_delta",
                        JsonSerializer.Serialize(thinkingDeltaEvent, ThorJsonSerializer.DefaultOptions),
                        thinkingDeltaEvent);
                }

                // 处理结束
                if (!string.IsNullOrEmpty(choice.FinishReason) && !isFinished)
                {
                    isFinished = true;

                    // 发送content_block_stop事件（如果有活跃的内容块）
                    if (!string.IsNullOrEmpty(currentContentBlockType))
                    {
                        var contentBlockStopEvent = CreateContentBlockStopEvent();
                        contentBlockStopEvent.Index = currentBlockIndex;
                        yield return ("content_block_stop",
                            JsonSerializer.Serialize(contentBlockStopEvent, ThorJsonSerializer.DefaultOptions),
                            contentBlockStopEvent);
                    }

                    // 发送message_delta事件
                    var messageDeltaEvent = CreateMessageDeltaEvent(
                        GetStopReasonByLastContentType(choice.FinishReason, lastContentBlockType), accumulatedUsage);

                    // 记录最终Usage统计
                    _logger.LogDebug(
                        "流式响应结束，最终Usage: Input={InputTokens}, Output={OutputTokens}, CacheRead={CacheRead}",
                        accumulatedUsage.InputTokens, accumulatedUsage.OutputTokens,
                        accumulatedUsage.CacheReadInputTokens);

                    yield return ("message_delta",
                        JsonSerializer.Serialize(messageDeltaEvent, ThorJsonSerializer.DefaultOptions),
                        messageDeltaEvent);

                    // 发送message_stop事件
                    var messageStopEvent = CreateMessageStopEvent();
                    yield return ("message_stop",
                        JsonSerializer.Serialize(messageStopEvent, ThorJsonSerializer.DefaultOptions),
                        messageStopEvent);
                }
            }
        }

        // 确保流正确结束
        if (!isFinished)
        {
            if (!string.IsNullOrEmpty(currentContentBlockType))
            {
                var contentBlockStopEvent = CreateContentBlockStopEvent();
                contentBlockStopEvent.Index = currentBlockIndex;
                yield return ("content_block_stop",
                    JsonSerializer.Serialize(contentBlockStopEvent, ThorJsonSerializer.DefaultOptions),
                    contentBlockStopEvent);
            }

            var messageDeltaEvent =
                CreateMessageDeltaEvent(GetStopReasonByLastContentType("end_turn", lastContentBlockType),
                    accumulatedUsage);
            yield return ("message_delta",
                JsonSerializer.Serialize(messageDeltaEvent, ThorJsonSerializer.DefaultOptions), messageDeltaEvent);

            var messageStopEvent = CreateMessageStopEvent();
            yield return ("message_stop", JsonSerializer.Serialize(messageStopEvent, ThorJsonSerializer.DefaultOptions),
                messageStopEvent);
        }
    }

    /// <summary>
    ///     将AnthropicInput转换为ThorChatCompletionsRequest
    /// </summary>
    private ThorChatCompletionsRequest ConvertAnthropicToOpenAI(AnthropicInput anthropicInput)
    {
        var openAiRequest = new ThorChatCompletionsRequest
        {
            Model = anthropicInput.Model,
            MaxTokens = anthropicInput.MaxTokens,
            Stream = anthropicInput.Stream,
            Messages = new List<ThorChatMessage>()
        };

        if (anthropicInput.Thinking != null &&
            anthropicInput.Thinking.Type.Equals("enabled", StringComparison.OrdinalIgnoreCase))
        {
            openAiRequest.Thinking = new ThorChatClaudeThinking
            {
                BudgetToken = anthropicInput.Thinking.BudgetTokens,
                Type = "enabled"
            };
            openAiRequest.EnableThinking = true;
        }

        if (!string.IsNullOrEmpty(anthropicInput.System))
            openAiRequest.Messages.Add(ThorChatMessage.CreateSystemMessage(anthropicInput.System));

        if (anthropicInput.Systems?.Count > 0)
            foreach (var systemContent in anthropicInput.Systems)
                openAiRequest.Messages.Add(ThorChatMessage.CreateSystemMessage(systemContent.Text ?? string.Empty));


        // 处理messages
        if (anthropicInput.Messages != null)
        {
            foreach (var message in anthropicInput.Messages)
            {
                var thorMessages = ConvertAnthropicMessageToThor(message);
                // 需要过滤 空消息
                if (thorMessages.Count == 0) continue;

                openAiRequest.Messages.AddRange(thorMessages);
            }

            openAiRequest.Messages = openAiRequest.Messages
                .Where(m => !string.IsNullOrEmpty(m.Content) || m.Contents?.Count > 0 || m.ToolCalls?.Count > 0 ||
                            !string.IsNullOrEmpty(m.ToolCallId))
                .ToList();
        }

        // 处理tools
        if (anthropicInput.Tools is { Count: > 0 })
            openAiRequest.Tools = anthropicInput.Tools.Select(ConvertAnthropicToolToThor).ToList();

        // 处理tool_choice
        if (anthropicInput.ToolChoice != null)
            openAiRequest.ToolChoice = ConvertAnthropicToolChoiceToThor(anthropicInput.ToolChoice);

        return openAiRequest;
    }

    /// <summary>
    ///     转换Anthropic消息为Thor消息列表
    /// </summary>
    private static List<ThorChatMessage> ConvertAnthropicMessageToThor(AnthropicMessageInput anthropicMessage)
    {
        var results = new List<ThorChatMessage>();

        // 处理简单的字符串内容
        if (anthropicMessage.Content != null)
        {
            var thorMessage = new ThorChatMessage
            {
                Role = anthropicMessage.Role,
                Content = anthropicMessage.Content
            };
            results.Add(thorMessage);
            return results;
        }

        // 处理多模态内容
        if (anthropicMessage.Contents is { Count: > 0 })
        {
            var currentContents = new List<ThorChatMessageContent>();
            var currentToolCalls = new List<ThorToolCall>();

            foreach (var content in anthropicMessage.Contents)
                if (content.Type == "text")
                {
                    currentContents.Add(ThorChatMessageContent.CreateTextContent(content.Text ?? string.Empty));
                }
                else if (content.Type == "image")
                {
                    if (content.Source != null)
                    {
                        var imageUrl = content.Source.Type == "base64"
                            ? $"data:{content.Source.MediaType};base64,{content.Source.Data}"
                            : content.Source.Data;
                        currentContents.Add(ThorChatMessageContent.CreateImageUrlContent(imageUrl ?? string.Empty));
                    }
                }
                else if (content.Type == "tool_use")
                {
                    // 如果有普通内容，先创建内容消息
                    if (currentContents.Count > 0)
                    {
                        if (currentContents.Count == 1 && currentContents.Any(x => x.Type == "text"))
                        {
                            var contentMessage = new ThorChatMessage
                            {
                                Role = anthropicMessage.Role,
                                ContentCalculated = currentContents.FirstOrDefault()?.Text ?? string.Empty
                            };
                            results.Add(contentMessage);
                        }
                        else
                        {
                            var contentMessage = new ThorChatMessage
                            {
                                Role = anthropicMessage.Role,
                                Contents = currentContents
                            };
                            results.Add(contentMessage);
                        }

                        currentContents = new List<ThorChatMessageContent>();
                    }

                    // 收集工具调用
                    var toolCall = new ThorToolCall
                    {
                        Id = content.Id,
                        Type = "function",
                        Function = new ThorChatMessageFunction
                        {
                            Name = content.Name,
                            Arguments = JsonSerializer.Serialize(content.Input)
                        }
                    };
                    currentToolCalls.Add(toolCall);
                }
                else if (content.Type == "tool_result")
                {
                    // 如果有普通内容，先创建内容消息
                    if (currentContents.Count > 0)
                    {
                        var contentMessage = new ThorChatMessage
                        {
                            Role = anthropicMessage.Role,
                            Contents = currentContents
                        };
                        results.Add(contentMessage);
                        currentContents = new List<ThorChatMessageContent>();
                    }

                    // 如果有工具调用，先创建工具调用消息
                    if (currentToolCalls.Count > 0)
                    {
                        var toolCallMessage = new ThorChatMessage
                        {
                            Role = anthropicMessage.Role,
                            ToolCalls = currentToolCalls
                        };
                        results.Add(toolCallMessage);
                        currentToolCalls = new List<ThorToolCall>();
                    }

                    // 创建工具结果消息
                    var toolMessage = new ThorChatMessage
                    {
                        Role = "tool",
                        ToolCallId = content.ToolUseId,
                        Content = content.Content?.ToString() ?? string.Empty
                    };
                    results.Add(toolMessage);
                }

            // 处理剩余的内容
            if (currentContents.Count > 0)
            {
                var contentMessage = new ThorChatMessage
                {
                    Role = anthropicMessage.Role,
                    Contents = currentContents
                };
                results.Add(contentMessage);
            }

            // 处理剩余的工具调用
            if (currentToolCalls.Count > 0)
            {
                var toolCallMessage = new ThorChatMessage
                {
                    Role = anthropicMessage.Role,
                    ToolCalls = currentToolCalls
                };
                results.Add(toolCallMessage);
            }
        }

        // 如果没有任何内容，返回一个空的消息
        if (results.Count == 0)
            results.Add(new ThorChatMessage
            {
                Role = anthropicMessage.Role,
                Content = string.Empty
            });

        // 如果只有一个text则使用content字段
        if (results is [{ Contents.Count: 1 }] &&
            results.FirstOrDefault()?.Contents?.FirstOrDefault()?.Type == "text" &&
            !string.IsNullOrEmpty(results.FirstOrDefault()?.Contents?.FirstOrDefault()?.Text))
            return
            [
                new ThorChatMessage
                {
                    Role = results[0].Role,
                    Content = results.FirstOrDefault()?.Contents?.FirstOrDefault()?.Text ?? string.Empty
                }
            ];

        return results;
    }

    /// <summary>
    ///     转换Anthropic工具为Thor工具
    /// </summary>
    private ThorToolDefinition ConvertAnthropicToolToThor(AnthropicMessageTool anthropicTool)
    {
        IDictionary<string, ThorToolFunctionPropertyDefinition> values =
            new Dictionary<string, ThorToolFunctionPropertyDefinition>();

        if (anthropicTool.InputSchema?.Properties != null)
            foreach (var property in anthropicTool.InputSchema.Properties)
                if (property.Value?.description != null)
                {
                    var definitionType = new ThorToolFunctionPropertyDefinition
                    {
                        Description = property.Value.description,
                        Type = property.Value.type
                    };
                    if (property.Value?.items?.type != null)
                        definitionType.Items = new ThorToolFunctionPropertyDefinition
                        {
                            Type = property.Value.items.type
                        };

                    values.Add(property.Key, definitionType);
                }


        return new ThorToolDefinition
        {
            Type = "function",
            Function = new ThorToolFunctionDefinition
            {
                Name = anthropicTool.name,
                Description = anthropicTool.Description,
                Parameters = new ThorToolFunctionPropertyDefinition
                {
                    Type = anthropicTool.InputSchema?.Type ?? "object",
                    Properties = values,
                    Required = anthropicTool.InputSchema?.Required
                }
            }
        };
    }

    /// <summary>
    ///     转换Anthropic工具选择为Thor工具选择
    /// </summary>
    private ThorToolChoice ConvertAnthropicToolChoiceToThor(AnthropicTooChoiceInput anthropicToolChoice)
    {
        return new ThorToolChoice
        {
            Type = anthropicToolChoice.Type ?? "auto",
            Function = anthropicToolChoice.Name != null
                ? new ThorToolChoiceFunctionTool { Name = anthropicToolChoice.Name }
                : null
        };
    }

    /// <summary>
    ///     将OpenAI响应转换为Claude响应格式
    /// </summary>
    private AnthropicChatCompletionDto ConvertOpenAIToClaude(ThorChatCompletionsResponse openAIResponse,
        AnthropicInput originalRequest)
    {
        var claudeResponse = new AnthropicChatCompletionDto
        {
            id = openAIResponse.Id,
            type = "message",
            role = "assistant",
            model = openAIResponse.Model ?? originalRequest.Model,
            stop_reason = GetClaudeStopReason(openAIResponse.Choices?.FirstOrDefault()?.FinishReason),
            stop_sequence = "",
            content = []
        };

        if (openAIResponse.Choices is { Count: > 0 })
        {
            var choice = openAIResponse.Choices.First();
            var contents = new List<AnthropicChatCompletionDtoContent>();

            if (!string.IsNullOrEmpty(choice.Message.Content) && !string.IsNullOrEmpty(choice.Message.ReasoningContent))
            {
                contents.Add(new AnthropicChatCompletionDtoContent
                {
                    type = "thinking",
                    Thinking = choice.Message.ReasoningContent
                });

                contents.Add(new AnthropicChatCompletionDtoContent
                {
                    type = "text",
                    text = choice.Message.Content
                });
            }
            else
            {
                // 处理思维内容
                if (!string.IsNullOrEmpty(choice.Message.ReasoningContent))
                    contents.Add(new AnthropicChatCompletionDtoContent
                    {
                        type = "thinking",
                        Thinking = choice.Message.ReasoningContent
                    });

                // 处理文本内容
                if (!string.IsNullOrEmpty(choice.Message.Content))
                    contents.Add(new AnthropicChatCompletionDtoContent
                    {
                        type = "text",
                        text = choice.Message.Content
                    });
            }

            // 处理工具调用
            if (choice.Message.ToolCalls is { Count: > 0 })
                contents.AddRange(choice.Message.ToolCalls.Select(toolCall => new AnthropicChatCompletionDtoContent
                {
                    type = "tool_use", id = toolCall.Id, name = toolCall.Function?.Name,
                    input = JsonSerializer.Deserialize<object>(toolCall.Function?.Arguments ?? "{}")
                }));

            claudeResponse.content = contents.ToArray();
        }

        // 处理使用情况统计 - 确保始终提供Usage信息
        claudeResponse.Usage = new AnthropicCompletionDtoUsage
        {
            InputTokens = openAIResponse.Usage?.PromptTokens ?? 0,
            OutputTokens = (int?)(openAIResponse.Usage?.CompletionTokens ?? 0),
            CacheCreationInputTokens = openAIResponse.Usage?.PromptTokensDetails?.CachedTokens ?? 0,
            CacheReadInputTokens = openAIResponse.Usage?.PromptTokensDetails?.CachedTokens ?? 0
        };

        // 记录Usage统计日志
        _logger.LogDebug("非流式响应Usage: Input={InputTokens}, Output={OutputTokens}, CacheRead={CacheRead}",
            claudeResponse.Usage.InputTokens, claudeResponse.Usage.OutputTokens,
            claudeResponse.Usage.CacheReadInputTokens);

        return claudeResponse;
    }


    /// <summary>
    ///     将OpenAI的完成原因转换为Claude的停止原因
    /// </summary>
    private string GetClaudeStopReason(string? openAiFinishReason)
    {
        return openAiFinishReason switch
        {
            "stop" => "end_turn",
            "length" => "max_tokens",
            "tool_calls" => "tool_use",
            "content_filter" => "stop_sequence",
            _ => "end_turn"
        };
    }

    /// <summary>
    ///     根据最后的内容块类型和OpenAI的完成原因确定Claude的停止原因
    /// </summary>
    private string GetStopReasonByLastContentType(string? openAiFinishReason, string lastContentBlockType)
    {
        // 如果最后一个内容块是工具调用，优先返回tool_use
        if (lastContentBlockType == "tool_use") return "tool_use";

        // 否则使用标准的转换逻辑
        return GetClaudeStopReason(openAiFinishReason);
    }
}