using ClaudeCodeProxy.Domain;
using Thor.Abstractions.Responses.Dto;

namespace Thor.Abstractions.Responses;

public interface IThorResponsesService
{
    /// <summary>
    ///     同步获取响应
    /// </summary>
    /// <param name="input"></param>
    /// <param name="options"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ResponsesDto> GetResponseAsync(ResponsesInput input,
        Dictionary<string, string> headers,
        ProxyConfig? config,
        ThorPlatformOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="input"></param>
    /// <param name="headers"></param>
    /// <param name="config"></param>
    /// <param name="options"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ResponsesDto> GetStreamToResponseAsync(ResponsesInput input,
        Dictionary<string, string> headers,
        ProxyConfig? config,
        ThorPlatformOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// </summary>
    /// <param name="input"></param>
    /// <param name="headers"></param>
    /// <param name="config"></param>
    /// <param name="options"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    IAsyncEnumerable<(string @event, ResponsesSSEDto<ResponsesDto> responses)> GetResponsesAsync(ResponsesInput input,
        Dictionary<string, string> headers,
        ProxyConfig? config,
        ThorPlatformOptions? options = null,
        CancellationToken cancellationToken = default);
}