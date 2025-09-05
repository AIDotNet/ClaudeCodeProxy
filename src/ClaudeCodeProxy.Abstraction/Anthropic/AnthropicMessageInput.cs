using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClaudeCodeProxy.Abstraction.Anthropic;

public class AnthropicMessageInput
{
    [JsonIgnore] public string? Content;

    [JsonIgnore] public IList<AnthropicMessageContent>? Contents;
    [JsonPropertyName("role")] public string Role { get; set; }

    [JsonPropertyName("content")]
    public object? ContentCalculated
    {
        get
        {
            if (Content is not null && Contents is not null)
                throw new ValidationException("Messages 中 Content 和 Contents 字段不能同时有值");

            if (Content is not null) return Content;

            return Contents!;
        }
        set
        {
            if (value is JsonElement str)
            {
                if (str.ValueKind == JsonValueKind.String)
                    Content = value?.ToString();
                else if (str.ValueKind == JsonValueKind.Array)
                    Contents = JsonSerializer.Deserialize<IList<AnthropicMessageContent>>(value?.ToString(),
                        ThorJsonSerializer.DefaultOptions);
            }
            else
            {
                Content = value?.ToString();
            }
        }
    }

    [JsonPropertyName("cache_control")] public AnthropicCacheControls? CacheControl { get; set; }

    [JsonPropertyName("signature")] public string? Signature { get; set; }

    [JsonPropertyName("thinking")] public string? Thinking { get; set; }
}