using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClaudeCodeProxy.Abstraction.Anthropic;

public sealed class AnthropicInput
{
    [JsonPropertyName("stream")] public bool Stream { get; set; }

    [JsonPropertyName("model")] public string Model { get; set; }

    [JsonPropertyName("max_tokens")] public int? MaxTokens { get; set; }

    [JsonPropertyName("messages")] public IList<AnthropicMessageInput> Messages { get; set; }

    [JsonPropertyName("tools")] public IList<AnthropicMessageTool>? Tools { get; set; }

    [JsonPropertyName("betas")] public object? Betas { get; set; }

    [JsonPropertyName("tool_choice")]
    public object ToolChoiceCalculated
    {
        get
        {
            if (string.IsNullOrEmpty(ToolChoiceString)) return ToolChoiceString;

            if (ToolChoice?.Type == "function") return ToolChoice;

            return ToolChoice?.Type;
        }
        set
        {
            if (value is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.String)
                    ToolChoiceString = jsonElement.GetString();
                else if (jsonElement.ValueKind == JsonValueKind.Object)
                    ToolChoice = jsonElement.Deserialize<AnthropicTooChoiceInput>(ThorJsonSerializer.DefaultOptions);
            }
            else
            {
                ToolChoice = (AnthropicTooChoiceInput)value;
            }
        }
    }

    [JsonIgnore] public string? ToolChoiceString { get; set; }

    [JsonIgnore] public AnthropicTooChoiceInput? ToolChoice { get; set; }

    [JsonIgnore] public IList<AnthropicMessageContent>? Systems { get; set; }

    [JsonIgnore] public string? System { get; set; }

    [JsonPropertyName("system")]
    public object SystemCalculated
    {
        get
        {
            if (System is not null && Systems is not null) throw new ValidationException("System 和 Systems 字段不能同时有值");

            if (System is not null) return System;

            return Systems!;
        }
        set
        {
            if (value is JsonElement str)
            {
                if (str.ValueKind == JsonValueKind.String)
                    System = value?.ToString();
                else if (str.ValueKind == JsonValueKind.Array)
                    Systems = JsonSerializer.Deserialize<IList<AnthropicMessageContent>>(value?.ToString(),
                        ThorJsonSerializer.DefaultOptions);
            }
            else
            {
                System = value?.ToString();
            }
        }
    }

    [JsonPropertyName("thinking")] public AnthropicThinkingInput? Thinking { get; set; }

    [JsonPropertyName("temperature")] public double? Temperature { get; set; }

    [JsonPropertyName("metadata")] public Dictionary<string, object>? Metadata { get; set; }

    [JsonPropertyName("stop_reason")] public string? StopReason { get; set; }
    
    [JsonPropertyName("mcp_servers")] public AnthropicMcpServersInput? McpServers { get; set; }
}

public class AnthropicMcpServersInput
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("url")]
    public string? Url { get; set; }
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("authorization_token")]
    public string? AuthorizationToken { get; set; }
    
    [JsonPropertyName("tool_configuration")]
    public AnthropicMcpServersToolConfigurationInput? ToolConfiguration { get; set; }
}

public class AnthropicMcpServersToolConfigurationInput
{
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; set; }
    
    [JsonPropertyName("allowed_tools")]
    public string[]? AllowedTools { get; set; }
}

public class AnthropicThinkingInput
{
    [JsonPropertyName("type")] public string Type { get; set; }

    [JsonPropertyName("budget_tokens")] public int BudgetTokens { get; set; }
}

public class AnthropicTooChoiceInput
{
    [JsonPropertyName("type")] public string? Type { get; set; }

    [JsonPropertyName("name")] public string? Name { get; set; }
}

public class AnthropicMessageTool
{
    [JsonPropertyName("type")] public string? Type { get; set; }

    [JsonPropertyName("user_location")] public object? UserLocation { get; set; }

    [JsonPropertyName("name")] public string name { get; set; }

    [JsonPropertyName("description")] public string? Description { get; set; }

    [JsonPropertyName("input_schema")] public InputSchema? InputSchema { get; set; }

    [JsonPropertyName("max_uses")] public string? MaxUses { get; set; }

    [JsonPropertyName("allowed_domains")] public string[]? AllowedDomains { get; set; }

    [JsonPropertyName("blocked_domains")] public string[]? BlockedDomains { get; set; }
}

public class InputSchema
{
    [JsonPropertyName("type")] public string Type { get; set; }

    [JsonPropertyName("properties")] public Dictionary<string, InputSchemaValue>? Properties { get; set; }

    [JsonPropertyName("required")] public string[]? Required { get; set; }
}

public class InputSchemaValue
{
    public string type { get; set; }

    public string description { get; set; }

    public InputSchemaValue? items { get; set; }
}