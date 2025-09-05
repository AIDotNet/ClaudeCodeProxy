using System.Text.Json.Serialization;

namespace ClaudeCodeProxy.Abstraction.Anthropic;

public class AnthropicCitationsItem
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
}


public class AnthropicCitations
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("cited_text")]
    public string? CitedText { get; set; }
    
    [JsonPropertyName("document_index")]
    public int? DocumentIndex { get; set; }
    
    [JsonPropertyName("document_title")]
    public string? DocumentTitle { get; set; }
    
    [JsonPropertyName("start_char_index")]
    public int? StartCharIndex { get; set; }
    
    [JsonPropertyName("end_char_index")]
    public int? EndCharIndex { get; set; }
}

