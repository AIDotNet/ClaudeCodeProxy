using ClaudeCodeProxy.Abstraction.Anthropic;

namespace ClaudeCodeProxy.Core.AI;

public abstract class AnthropicBase
{
    /// <summary>
    ///     创建content_block_start事件
    /// </summary>
    protected AnthropicStreamDto CreateContentBlockStartEvent()
    {
        return new AnthropicStreamDto
        {
            Type = "content_block_start",
            Index = 0,
            ContentBlock = new AnthropicChatCompletionDtoContentBlock
            {
                Type = "text",
                Id = null,
                Name = null
            }
        };
    }

    /// <summary>
    ///     创建thinking block start事件
    /// </summary>
    protected AnthropicStreamDto CreateThinkingBlockStartEvent()
    {
        return new AnthropicStreamDto
        {
            Type = "content_block_start",
            Index = 0,
            ContentBlock = new AnthropicChatCompletionDtoContentBlock
            {
                Type = "thinking",
                Id = null,
                Name = null
            }
        };
    }

    /// <summary>
    ///     创建content_block_delta事件
    /// </summary>
    protected AnthropicStreamDto CreateContentBlockDeltaEvent(string text)
    {
        return new AnthropicStreamDto
        {
            Type = "content_block_delta",
            Index = 0,
            Delta = new AnthropicChatCompletionDtoDelta
            {
                Type = "text_delta",
                Text = text
            }
        };
    }

    /// <summary>
    ///     创建thinking delta事件
    /// </summary>
    protected AnthropicStreamDto CreateThinkingBlockDeltaEvent(string thinking)
    {
        return new AnthropicStreamDto
        {
            Type = "content_block_delta",
            Index = 0,
            Delta = new AnthropicChatCompletionDtoDelta
            {
                Type = "thinking",
                Thinking = thinking
            }
        };
    }

    /// <summary>
    ///     创建content_block_stop事件
    /// </summary>
    protected AnthropicStreamDto CreateContentBlockStopEvent()
    {
        return new AnthropicStreamDto
        {
            Type = "content_block_stop",
            Index = 0
        };
    }

    /// <summary>
    ///     创建message_delta事件
    /// </summary>
    protected AnthropicStreamDto CreateMessageDeltaEvent(string finishReason, AnthropicCompletionDtoUsage usage)
    {
        return new AnthropicStreamDto
        {
            Type = "message_delta",
            Usage = usage,
            Delta = new AnthropicChatCompletionDtoDelta
            {
                StopReason = finishReason
            }
        };
    }

    /// <summary>
    ///     创建message_stop事件
    /// </summary>
    protected AnthropicStreamDto CreateMessageStopEvent()
    {
        return new AnthropicStreamDto
        {
            Type = "message_stop"
        };
    }

    /// <summary>
    ///     创建tool block start事件
    /// </summary>
    protected AnthropicStreamDto CreateToolBlockStartEvent(string? toolId, string? toolName)
    {
        return new AnthropicStreamDto
        {
            Type = "content_block_start",
            Index = 0,
            ContentBlock = new AnthropicChatCompletionDtoContentBlock
            {
                Type = "tool_use",
                Id = toolId,
                Name = toolName
            }
        };
    }

    /// <summary>
    ///     创建tool delta事件
    /// </summary>
    protected AnthropicStreamDto CreateToolBlockDeltaEvent(string partialJson)
    {
        return new AnthropicStreamDto
        {
            Type = "content_block_delta",
            Index = 0,
            Delta = new AnthropicChatCompletionDtoDelta
            {
                Type = "input_json_delta",
                PartialJson = partialJson
            }
        };
    }

    /// <summary>
    ///     创建message_start事件
    /// </summary>
    protected AnthropicStreamDto CreateMessageStartEvent(string messageId, string model)
    {
        return new AnthropicStreamDto
        {
            Type = "message_start",
            Message = new AnthropicChatCompletionDto
            {
                id = messageId,
                type = "message",
                role = "assistant",
                model = model,
                content = new AnthropicChatCompletionDtoContent[0],
                Usage = new AnthropicCompletionDtoUsage
                {
                    InputTokens = 0,
                    OutputTokens = 0,
                    CacheCreationInputTokens = 0,
                    CacheReadInputTokens = 0
                }
            }
        };
    }
}