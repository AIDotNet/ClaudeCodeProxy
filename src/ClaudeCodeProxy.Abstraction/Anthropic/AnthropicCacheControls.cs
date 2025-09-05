using System.Text.Json.Serialization;

namespace ClaudeCodeProxy.Abstraction.Anthropic;

public sealed class AnthropicCacheControls
{
    [JsonPropertyName("type")] public string? Type { get; set; }
}