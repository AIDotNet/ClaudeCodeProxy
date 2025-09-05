using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using ClaudeCodeProxy.Abstraction;
using ClaudeCodeProxy.Abstraction.Anthropic;
using ClaudeCodeProxy.Domain;
using Microsoft.Extensions.Logging;
using Thor.Abstractions;
using Thor.Abstractions.Chats.Dtos;
using Thor.Abstractions.Responses;
using Thor.Abstractions.Responses.Dto;

namespace ClaudeCodeProxy.Core.AI;

public class OpenAiAnthropicResponsesService(
    IThorResponsesService thorResponsesService,
    ILogger<OpenAiAnthropicResponsesService> logger) : AnthropicBase
{
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
            var openAIRequest = ConvertAnthropicToOpenAiResponses(input);

            // 调用OpenAI服务
            var openAIResponse =
                await thorResponsesService.GetResponseAsync(openAIRequest, headers, config, options, cancellationToken);

            // 转换响应格式：OpenAI -> Claude
            var claudeResponse = ConvertOpenAIToClaude(openAIResponse, input);

            return claudeResponse;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OpenAI到Claude适配器异常");
            throw;
        }
    }

    /// <summary>
    ///     流式对话补全
    /// </summary>
    public async IAsyncEnumerable<(string?, AnthropicStreamDto?)> StreamChatCompletionsAsync(AnthropicInput input,
        Dictionary<string, string> headers,
        ProxyConfig? config,
        ThorPlatformOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var openAIRequest = ConvertAnthropicToOpenAiResponses(input);
        openAIRequest.Stream = true;

        var messageId = Guid.NewGuid().ToString();
        var hasStarted = false;
        var accumulatedUsage = new AnthropicCompletionDtoUsage();
        var isFinished = false;
        var currentContentBlockType = "";
        var currentBlockIndex = 0;
        var toolCallsStarted = new Dictionary<string, bool>(); // 跟踪工具调用的开始状态
        var lastContentBlockType = ""; // 跟踪最后的内容块类型

        await foreach (var streamResponse in thorResponsesService.GetResponsesAsync(openAIRequest, headers, config,
                           options, cancellationToken))
        {
            var eventType = streamResponse.Item1;
            var sseData = streamResponse.Item2;

            // 记录所有接收到的事件类型用于调试
            logger.LogDebug("Received OpenAI Responses event: {EventType}", eventType);

            // 发送message_start事件
            if (!hasStarted && (eventType == "response.created" || eventType == "response.output_text.delta"))
            {
                hasStarted = true;
                var messageStartEvent = CreateMessageStartEvent(messageId, input.Model);
                yield return ("message_start", messageStartEvent);
            }

            // 更新使用情况统计
            if (sseData.Response?.Usage != null)
            {
                var usage = sseData.Response.Usage;
                if (usage.InputTokens > 0)
                    accumulatedUsage.InputTokens = usage.InputTokens;
                if (usage.OutputTokens > 0)
                    accumulatedUsage.OutputTokens = usage.OutputTokens;
                if (usage.InputTokensDetails?.CachedTokens > 0)
                    accumulatedUsage.CacheReadInputTokens = usage.InputTokensDetails.CachedTokens;

                // 处理reasoning tokens - OpenAI Responses API 2025新特性
                if (usage.OutputTokensDetails?.ReasoningTokens > 0)
                    // Claude没有直接等价的reasoning tokens字段，我们将其加到output_tokens中
                    // 但可以通过日志记录具体的reasoning token使用情况
                    logger.LogDebug("OpenAI Reasoning Tokens: {ReasoningTokens}",
                        usage.OutputTokensDetails.ReasoningTokens);
            }

            // 处理文本增量 - 支持多种OpenAI Responses API 2025事件类型
            if (!string.IsNullOrEmpty(sseData.Delta) ||
                eventType == "response.output_text.delta" ||
                eventType == "response.text.delta")
            {
                // 如果当前有其他类型的内容块在运行，先结束它们
                if (currentContentBlockType != "text" && !string.IsNullOrEmpty(currentContentBlockType))
                {
                    var stopEvent = CreateContentBlockStopEvent();
                    stopEvent.Index = currentBlockIndex;
                    yield return ("content_block_stop",
                        stopEvent);
                    currentBlockIndex++;
                    currentContentBlockType = "";
                }

                // 发送content_block_start事件（仅第一次）
                if (currentContentBlockType != "text")
                {
                    currentContentBlockType = "text";
                    lastContentBlockType = "text";
                    var contentBlockStartEvent = CreateContentBlockStartEvent();
                    contentBlockStartEvent.Index = currentBlockIndex;
                    yield return ("content_block_start",
                        contentBlockStartEvent);
                }

                // 发送content_block_delta事件
                var contentDeltaEvent = CreateContentBlockDeltaEvent(sseData.Delta);
                contentDeltaEvent.Index = currentBlockIndex;
                yield return ("content_block_delta",
                    contentDeltaEvent);
            }

            // 处理工具调用 - 基于OpenAI Responses API的function_call或tool_call事件
            if (sseData.Item?.Type == "function_call" || eventType == "response.function_call.delta")
            {
                // 结束当前的文本或reasoning内容块
                if (currentContentBlockType == "text" || currentContentBlockType == "thinking")
                {
                    var stopEvent = CreateContentBlockStopEvent();
                    stopEvent.Index = currentBlockIndex;
                    yield return ("content_block_stop", stopEvent);
                    currentBlockIndex++;
                }

                var functionCallId = sseData.Item?.Id ?? sseData.ItemId ?? Guid.NewGuid().ToString();

                // 记录item_id用于调试和跟踪
                if (!string.IsNullOrEmpty(sseData.ItemId))
                    logger.LogDebug("Processing function call with item_id: {ItemId}", sseData.ItemId);

                // 发送tool_use content_block_start事件
                if (toolCallsStarted.TryAdd(functionCallId, true))
                {
                    currentContentBlockType = "tool_use";
                    lastContentBlockType = "tool_use";

                    var toolBlockStartEvent =
                        CreateToolBlockStartEvent(functionCallId, sseData.Item?.Content?[0]?.Text);
                    toolBlockStartEvent.Index = currentBlockIndex;
                    yield return ("content_block_start",
                        toolBlockStartEvent);
                }

                // 处理function call参数的增量更新
                if (sseData.Item?.Content?.Length > 0)
                    foreach (var content in sseData.Item.Content)
                        if (!string.IsNullOrEmpty(content.Text))
                        {
                            var toolDeltaEvent = CreateToolBlockDeltaEvent(content.Text);
                            toolDeltaEvent.Index = currentBlockIndex;
                            yield return ("content_block_delta",
                                toolDeltaEvent);
                        }
            }

            // 处理reasoning内容 - OpenAI Responses API 2025的reasoning模型特性
            if (sseData.Item?.Type == "reasoning" || (eventType == "response.output_text.delta" &&
                                                      !string.IsNullOrEmpty(sseData.Response?.Reasoning?.Summary
                                                          ?.ToString())))
            {
                // 如果当前有其他类型的内容块在运行，先结束它们
                if (currentContentBlockType != "thinking" && !string.IsNullOrEmpty(currentContentBlockType))
                {
                    var stopEvent = CreateContentBlockStopEvent();
                    stopEvent.Index = currentBlockIndex;
                    yield return ("content_block_stop", stopEvent);
                    currentBlockIndex++;
                    currentContentBlockType = "";
                }

                // 发送thinking content_block_start事件（仅第一次）
                if (currentContentBlockType != "thinking")
                {
                    currentContentBlockType = "thinking";
                    lastContentBlockType = "thinking";
                    var thinkingBlockStartEvent = CreateThinkingBlockStartEvent();
                    thinkingBlockStartEvent.Index = currentBlockIndex;
                    yield return ("content_block_start",
                        thinkingBlockStartEvent);
                }

                // 处理reasoning内容的增量更新
                string? reasoningContent = null;
                if (sseData.Item?.Type == "reasoning" && sseData.Item.Content?.Length > 0)
                    reasoningContent = sseData.Item.Content[0]?.Text;
                else if (sseData.Response?.Reasoning?.Summary != null)
                    reasoningContent = ExtractReasoningContent(sseData.Response.Reasoning.Summary);

                if (!string.IsNullOrEmpty(reasoningContent))
                {
                    var thinkingDeltaEvent = CreateThinkingBlockDeltaEvent(reasoningContent);
                    thinkingDeltaEvent.Index = currentBlockIndex;
                    yield return ("content_block_delta",
                        thinkingDeltaEvent);
                }
            }

            // 处理各种完成状态 - 支持OpenAI Responses API 2025的多种事件类型
            if (eventType == "response.done" ||
                eventType == "response.output.item.done" ||
                sseData.Response?.Status == "completed" ||
                sseData.Response?.Status == "failed" ||
                sseData.Response?.Status == "incomplete")
            {
                isFinished = true;

                // 结束任何活跃的内容块
                if (!string.IsNullOrEmpty(currentContentBlockType))
                {
                    var contentBlockStopEvent = CreateContentBlockStopEvent();
                    contentBlockStopEvent.Index = currentBlockIndex;
                    yield return ("content_block_stop",
                        contentBlockStopEvent);
                }

                // 发送message_delta事件
                var messageDeltaEvent = CreateMessageDeltaEvent("end_turn", accumulatedUsage);
                yield return ("message_delta",
                    messageDeltaEvent);

                // 发送message_stop事件
                var messageStopEvent = CreateMessageStopEvent();
                yield return ("message_stop",
                    messageStopEvent);
                break;
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
                    contentBlockStopEvent);
            }

            var messageDeltaEvent = CreateMessageDeltaEvent("end_turn", accumulatedUsage);
            yield return ("message_delta", messageDeltaEvent);

            var messageStopEvent = CreateMessageStopEvent();
            yield return ("message_stop",
                messageStopEvent);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ResponsesInput ConvertAnthropicToOpenAiResponses(AnthropicInput input)
    {
        var openAiRequest = new ResponsesInput
        {
            Model = input.Model,
            Stream = input.Stream,
            Instructions = AIPrompt.CodeXPrompt
        };

        var inputMessages = new List<ResponsesMessageInput>();

        // 首先处理系统消息，将其作为角色为"system"的消息添加到消息列表开头
        if (!string.IsNullOrEmpty(input.System))
        {
            inputMessages.Add(new ResponsesMessageInput
            {
                Role = "system",
                Content = input.System
            });
        }
        else if (input.Systems?.Count > 0)
        {
            // 将多个系统消息合并为一个system角色的消息
            var systemTexts = input.Systems
                .Where(s => s.Type == "text" && !string.IsNullOrEmpty(s.Text) &&
                            s.Text != "You are Claude Code, Anthropic's official CLI for Claude.")
                .Select(s => s.Text);
            var combinedSystemMessage = string.Join("\n", systemTexts);
            if (!string.IsNullOrEmpty(combinedSystemMessage))
                inputMessages.Add(new ResponsesMessageInput
                {
                    Role = "system",
                    Content = combinedSystemMessage
                });
        }

        // 然后转换用户/助手消息格式：Claude messages -> OpenAI input
        if (input.Messages?.Count > 0)
            foreach (var message in input.Messages)
            {
                var responseMessage = new ResponsesMessageInput
                {
                    Role = message.Role
                };

                // 转换内容
                if (message.Content is string textContent)
                {
                    responseMessage.Content = textContent;
                }
                else if (message.ContentCalculated is IList<AnthropicMessageContent> contents)
                {
                    var convertedContents = new List<ResponsesMessageContentInput>();
                    foreach (var content in contents)
                    {
                        var contentInput = new ResponsesMessageContentInput();

                        if (content.Type == "text")
                        {
                            contentInput.Text = content.Text;
                            // 如果角色是ai则output_text
                            if (message.Role == "user")
                                contentInput.Type = "input_text";
                            else if (message.Role == "assistant")
                                contentInput.Type = "output_text";
                            else
                                contentInput.Type = "input_text";
                        }
                        else if (content.Type == "image")
                        {
                            // 处理图像内容
                            contentInput.Type = "input_image";
                            contentInput.ImageUrl = content.Source?.Data; // 需要根据实际结构调整
                        }
                        else if (content.Type == "tool_use")
                        {
                            // 工具调用转换为computer_call格式
                            contentInput.Type = "computer_call";
                            contentInput.Action = new
                            {
                                type = "function_call",
                                function = new
                                {
                                    name = content.Name,
                                    arguments = JsonSerializer.Serialize(content.Input)
                                }
                            };
                            contentInput.CallId = content.Id;
                            contentInput.Id = content.Id ?? Guid.NewGuid().ToString();
                            contentInput.PendingSafetyChecks = new object[0]; // 空的安全检查数组
                        }
                        else if (content.Type == "tool_result")
                        {
                            // 工具结果转换为computer_call_output格式
                            contentInput.Type = "computer_call_output";
                            contentInput.CallId = content.ToolUseId;
                            contentInput.Output = new
                            {
                                type = "function_output",
                                content = content.Content?.ToString() ?? string.Empty,
                                status = "completed"
                            };
                        }

                        // 如果所有内容都是空则不添加
                        if (string.IsNullOrEmpty(contentInput.Text) && string.IsNullOrEmpty(contentInput.ImageUrl) &&
                            string.IsNullOrEmpty(contentInput.Type))
                            continue;

                        convertedContents.Add(contentInput);
                    }

                    responseMessage.Contents = convertedContents;
                }

                inputMessages.Add(responseMessage);
            }

        // 设置输入消息列表
        openAiRequest.Inputs = inputMessages;

        // 转换工具
        if (input.Tools?.Count > 0)
            openAiRequest.Tools = input.Tools.Select(AnthropicMessageToolToResponsesToolsInput).ToList();

        // 转换工具选择
        if (input.Tools?.Count > 0) openAiRequest.ToolChoice = "auto";

        openAiRequest.ParallelToolCalls = true; // 支持并行工具调用
        openAiRequest.Store = false; // 默认不存储

        // 如果需要支持会话持续性，可以设置previous_response_id
        // 这需要从上下文或缓存中获取上一个响应的ID
        // openAIRequest.PreviousResponseId = GetPreviousResponseId(input);

        return openAiRequest;
    }

    private ResponsesToolsInput AnthropicMessageToolToResponsesToolsInput(AnthropicMessageTool tool)
    {
        var input = new ResponsesToolsInput
        {
            Type = "function",
            Description = tool.Description,
            Name = tool.name
        };

        if (tool.InputSchema != null)
        {
            input.Parameters = new ThorToolFunctionPropertyDefinition
            {
                Required = tool.InputSchema.Required?.ToArray() ?? [],
                Type = "object",
                Properties = new ConcurrentDictionary<string, ThorToolFunctionPropertyDefinition>()
            };
            if (tool.InputSchema.Properties != null)
                foreach (var schemaValue in tool.InputSchema.Properties)
                {
                    var definition = new ThorToolFunctionPropertyDefinition
                    {
                        Description = schemaValue.Value.description,
                        Type = schemaValue.Value.type,
                        Properties = new ConcurrentDictionary<string, ThorToolFunctionPropertyDefinition>()
                    };

                    if (schemaValue.Value.type == "array" && schemaValue.Value.items != null)
                        definition.Items = new ThorToolFunctionPropertyDefinition
                        {
                            Type = schemaValue.Value.items.type
                        };

                    input.Parameters.Properties.Add(schemaValue.Key, definition);
                    // 套娃
                }
        }

        return input;
    }

    private AnthropicChatCompletionDto ConvertOpenAIToClaude(ResponsesDto openAIResponse, AnthropicInput input)
    {
        var claudeResponse = new AnthropicChatCompletionDto
        {
            id = openAIResponse.Id ?? Guid.NewGuid().ToString(),
            type = "message",
            role = "assistant",
            model = openAIResponse.Model ?? input.Model,
            stop_reason = GetClaudeStopReason(openAIResponse.Status),
            stop_sequence = null
        };

        // 转换内容
        var contentList = new List<AnthropicChatCompletionDtoContent>();

        // 处理输出内容
        if (openAIResponse.Output?.Length > 0)
            foreach (var outputItem in openAIResponse.Output)
                // 处理角色为assistant的输出
                if (outputItem.Role == "assistant" && outputItem.Content?.Length > 0)
                {
                    foreach (var contentItem in outputItem.Content)
                    {
                        var content = new AnthropicChatCompletionDtoContent();

                        // 处理文本内容
                        if (contentItem.Type == "text")
                        {
                            content.type = "text";
                            content.text = contentItem.Text;
                            contentList.Add(content);
                        }
                    }
                }
                // 处理推理内容 (reasoning -> thinking)
                else if (outputItem.Type == "reasoning")
                {
                    var content = new AnthropicChatCompletionDtoContent
                    {
                        type = "thinking",
                        Thinking = ExtractReasoningContent(openAIResponse.Reasoning)
                    };

                    // 处理加密的推理内容 - OpenAI Responses API 2025新特性
                    if (!string.IsNullOrEmpty(outputItem.EncryptedContent))
                    {
                        logger.LogDebug("Found encrypted reasoning content: {Length} characters",
                            outputItem.EncryptedContent.Length);
                        // 加密内容目前无法直接解密，但可以记录其存在
                        content.signature = outputItem.EncryptedContent; // 使用signature字段存储加密内容
                    }

                    contentList.Add(content);
                }
                // 处理工具调用
                else if (outputItem.Type == "tool_calls" || outputItem.Type == "function")
                {
                    var content = new AnthropicChatCompletionDtoContent
                    {
                        type = "tool_use",
                        id = outputItem.CallId ?? Guid.NewGuid().ToString(),
                        name = outputItem.Name
                    };

                    // 解析工具参数
                    if (!string.IsNullOrEmpty(outputItem.Arguments))
                        try
                        {
                            content.input = JsonSerializer.Deserialize<object>(
                                outputItem.Arguments, ThorJsonSerializer.DefaultOptions);
                        }
                        catch
                        {
                            // 如果解析失败，将原始字符串作为输入
                            content.input = outputItem.Arguments;
                        }

                    contentList.Add(content);
                }

        // 如果没有内容，添加默认文本内容
        if (contentList.Count == 0 && !string.IsNullOrEmpty(openAIResponse.OutputText))
            contentList.Add(new AnthropicChatCompletionDtoContent
            {
                type = "text",
                text = openAIResponse.OutputText
            });

        claudeResponse.content = contentList.ToArray();

        // 转换使用统计
        claudeResponse.Usage = new AnthropicCompletionDtoUsage();
        if (openAIResponse.Usage != null)
        {
            claudeResponse.Usage.InputTokens = openAIResponse.Usage.InputTokens;
            claudeResponse.Usage.OutputTokens = openAIResponse.Usage.OutputTokens;

            // 转换缓存相关的token统计
            if (openAIResponse.Usage.InputTokensDetails?.CachedTokens > 0)
                claudeResponse.Usage.CacheReadInputTokens = openAIResponse.Usage.InputTokensDetails.CachedTokens;

            // 处理reasoning tokens - OpenAI Responses API 2025新特性
            if (openAIResponse.Usage.OutputTokensDetails?.ReasoningTokens > 0)
                logger.LogDebug("OpenAI Reasoning Tokens in response: {ReasoningTokens}",
                    openAIResponse.Usage.OutputTokensDetails.ReasoningTokens);
            // Claude不直接支持reasoning tokens字段，已包含在output_tokens中
        }

        return claudeResponse;
    }

    /// <summary>
    ///     从推理对象中提取推理内容
    /// </summary>
    private string? ExtractReasoningContent(object? reasoning)
    {
        if (reasoning == null) return null;

        // 如果是字符串直接返回
        if (reasoning is string reasoningStr)
            return reasoningStr;

        // 如果是对象，尝试提取文本内容
        if (reasoning is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String)
                return element.GetString();

            // 尝试提取常见的推理内容字段
            if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty("content", out var contentProp))
                    return contentProp.GetString();
                if (element.TryGetProperty("text", out var textProp))
                    return textProp.GetString();
                if (element.TryGetProperty("reasoning", out var reasoningProp))
                    return reasoningProp.GetString();
            }
        }

        // 最后尝试序列化整个对象
        try
        {
            return JsonSerializer.Serialize(reasoning, ThorJsonSerializer.DefaultOptions);
        }
        catch
        {
            return reasoning?.ToString();
        }
    }

    /// <summary>
    ///     将OpenAI的状态转换为Claude的停止原因
    /// </summary>
    private string GetClaudeStopReason(string? openAIStatus)
    {
        return openAIStatus switch
        {
            "completed" => "end_turn",
            "failed" => "max_tokens",
            "incomplete" => "max_tokens",
            "tool_calls" => "tool_use",
            _ => "end_turn"
        };
    }
}