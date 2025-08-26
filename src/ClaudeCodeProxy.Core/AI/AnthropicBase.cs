using Thor.Abstractions.Anthropic;

namespace ClaudeCodeProxy.Core.AI;

public abstract class AnthropicBase
{
    /// <summary>
    /// 创建content_block_start事件
    /// </summary>
    protected ClaudeStreamDto CreateContentBlockStartEvent()
    {
        return new ClaudeStreamDto
        {
            type = "content_block_start",
            index = 0,
            content_block = new ClaudeChatCompletionDtoContent_block
            {
                type = "text",
                id = null,
                name = null
            }
        };
    }

    /// <summary>
    /// 创建thinking block start事件
    /// </summary>
    protected  ClaudeStreamDto CreateThinkingBlockStartEvent()
    {
        return new ClaudeStreamDto
        {
            type = "content_block_start",
            index = 0,
            content_block = new ClaudeChatCompletionDtoContent_block
            {
                type = "thinking",
                id = null,
                name = null
            }
        };
    }

    /// <summary>
    /// 创建content_block_delta事件
    /// </summary>
    protected  ClaudeStreamDto CreateContentBlockDeltaEvent(string text)
    {
        return new ClaudeStreamDto
        {
            type = "content_block_delta",
            index = 0,
            delta = new ClaudeChatCompletionDtoDelta
            {
                type = "text_delta",
                text = text
            }
        };
    }

    /// <summary>
    /// 创建thinking delta事件
    /// </summary>
    protected  ClaudeStreamDto CreateThinkingBlockDeltaEvent(string thinking)
    {
        return new ClaudeStreamDto
        {
            type = "content_block_delta",
            index = 0,
            delta = new ClaudeChatCompletionDtoDelta
            {
                type = "thinking",
                thinking = thinking
            }
        };
    }

    /// <summary>
    /// 创建content_block_stop事件
    /// </summary>
    protected  ClaudeStreamDto CreateContentBlockStopEvent()
    {
        return new ClaudeStreamDto
        {
            type = "content_block_stop",
            index = 0
        };
    }

    /// <summary>
    /// 创建message_delta事件
    /// </summary>
    protected  ClaudeStreamDto CreateMessageDeltaEvent(string finishReason, ClaudeChatCompletionDtoUsage usage)
    {
        return new ClaudeStreamDto
        {
            type = "message_delta",
            Usage = usage,
            delta = new ClaudeChatCompletionDtoDelta
            {
                stop_reason = finishReason
            }
        };
    }

    /// <summary>
    /// 创建message_stop事件
    /// </summary>
    protected  ClaudeStreamDto CreateMessageStopEvent()
    {
        return new ClaudeStreamDto
        {
            type = "message_stop"
        };
    }

    /// <summary>
    /// 创建tool block start事件
    /// </summary>
    protected  ClaudeStreamDto CreateToolBlockStartEvent(string? toolId, string? toolName)
    {
        return new ClaudeStreamDto
        {
            type = "content_block_start",
            index = 0,
            content_block = new ClaudeChatCompletionDtoContent_block
            {
                type = "tool_use",
                id = toolId,
                name = toolName
            }
        };
    }

    /// <summary>
    /// 创建tool delta事件
    /// </summary>
    protected  ClaudeStreamDto CreateToolBlockDeltaEvent(string partialJson)
    {
        return new ClaudeStreamDto
        {
            type = "content_block_delta",
            index = 0,
            delta = new ClaudeChatCompletionDtoDelta
            {
                type = "input_json_delta",
                partial_json = partialJson
            }
        };
    }

    /// <summary>
    /// 创建message_start事件
    /// </summary>
    protected ClaudeStreamDto CreateMessageStartEvent(string messageId, string model)
    {
        return new ClaudeStreamDto
        {
            type = "message_start",
            message = new ClaudeChatCompletionDto
            {
                id = messageId,
                type = "message",
                role = "assistant",
                model = model,
                content = new ClaudeChatCompletionDtoContent[0],
                Usage = new ClaudeChatCompletionDtoUsage
                {
                    input_tokens = 0,
                    output_tokens = 0,
                    cache_creation_input_tokens = 0,
                    cache_read_input_tokens = 0
                }
            }
        };
    }
}