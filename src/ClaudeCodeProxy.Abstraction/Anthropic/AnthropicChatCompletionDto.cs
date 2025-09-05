using System.Text.Json.Serialization;

namespace ClaudeCodeProxy.Abstraction.Anthropic;

public class AnthropicStreamDto
{
    [JsonPropertyName("type")] public string? Type { get; set; }

    [JsonPropertyName("index")] public int? Index { get; set; }

    [JsonPropertyName("content_block")] public AnthropicChatCompletionDtoContentBlock? ContentBlock { get; set; }

    [JsonPropertyName("delta")] public AnthropicChatCompletionDtoDelta? Delta { get; set; }

    [JsonPropertyName("message")] public AnthropicChatCompletionDto? Message { get; set; }

    [JsonPropertyName("usage")] public AnthropicCompletionDtoUsage? Usage { get; set; }

    [JsonPropertyName("error")] public AnthropicStreamErrorDto? Error { get; set; }
}

public class AnthropicStreamErrorDto
{
    [JsonPropertyName("type")] public string? Type { get; set; }

    [JsonPropertyName("message")] public string? Message { get; set; }
}

public class AnthropicChatCompletionDtoDelta
{
    [JsonPropertyName("type")] public string? Type { get; set; }

    [JsonPropertyName("text")] public string? Text { get; set; }

    [JsonPropertyName("thinking")] public string? Thinking { get; set; }

    [JsonPropertyName("partial_json")] public string? PartialJson { get; set; }

    [JsonPropertyName("stop_reason")] public string? StopReason { get; set; }
}

public class AnthropicChatCompletionDtoContentBlock
{
    [JsonPropertyName("type")] public string? Type { get; set; }

    [JsonPropertyName("thinking")] public string? Thinking { get; set; }

    [JsonPropertyName("signature")] public string? Signature { get; set; }

    [JsonPropertyName("id")] public string? Id { get; set; }

    [JsonPropertyName("name")] public string? Name { get; set; }

    [JsonPropertyName("input")] public object? Input { get; set; }

    [JsonPropertyName("server_name")] public string? ServerName { get; set; }

    [JsonPropertyName("is_error")] public bool? IsError { get; set; }

    [JsonPropertyName("tool_use_id")] public string? ToolUseId { get; set; }

    [JsonPropertyName("content")] public object? Content { get; set; }
}

public class AnthropicChatCompletionDto
{
    public string id { get; set; }

    public string type { get; set; }

    public string role { get; set; }

    public AnthropicChatCompletionDtoContent[] content { get; set; }

    public string model { get; set; }

    public string stop_reason { get; set; }

    public object stop_sequence { get; set; }

    public AnthropicCompletionDtoUsage Usage { get; set; }
}

public class AnthropicChatCompletionDtoContent
{
    public string type { get; set; }

    public string? text { get; set; }

    public string? id { get; set; }

    public string? name { get; set; }

    public object? input { get; set; }

    [JsonPropertyName("thinking")] public string? Thinking { get; set; }

    public string? signature { get; set; }
}

public class AnthropicCompletionDtoUsage
{
    [JsonPropertyName("input_tokens")] public int? InputTokens { get; set; }

    [JsonPropertyName("cache_creation_input_tokens")]
    public int? CacheCreationInputTokens { get; set; }

    [JsonPropertyName("cache_read_input_tokens")]
    public int? CacheReadInputTokens { get; set; }

    [JsonPropertyName("output_tokens")] public int? OutputTokens { get; set; }

    [JsonPropertyName("server_tool_use")] public AnthropicServerToolUse? ServerToolUse { get; set; }
}

public class AnthropicServerToolUse
{
    [JsonPropertyName("web_search_requests")] 
    public int? WebSearchRequests { get; set; }
}