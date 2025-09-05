using System.Text.Json.Serialization;

namespace Thor.Abstractions.Dtos;

/// <summary>
/// </summary>
/// <typeparam name="T"></typeparam>
public record ThorDataBaseResponse<T> : ThorBaseResponse
{
    /// <summary>
    /// </summary>
    [JsonPropertyName("data")]
    public T? Data { get; set; }
}