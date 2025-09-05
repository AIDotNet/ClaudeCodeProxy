using System.Net.Http.Headers;
using System.Text.Json;
using ClaudeCodeProxy.Abstraction;

namespace ClaudeCodeProxy.Core.Extensions;

public static class HttpClientExtensions
{
    private static readonly MediaTypeHeaderValue JsonMediaType =
        new("application/json") { CharSet = "utf-8" };

    private static async ValueTask<HttpContent> CreateJsonContentAsync(object value)
    {
        var ms = new MemoryStream(16 * 1024); // 预分配减少扩容
        await JsonSerializer.SerializeAsync(ms, value, value.GetType(), ThorJsonSerializer.DefaultOptions)
            .ConfigureAwait(false);
        ms.Position = 0;

        var content = new StreamContent(ms);
        content.Headers.ContentType = JsonMediaType;
        return content;
    }

    public static async Task<HttpResponseMessage> HttpRequestRaw<T>(this HttpClient httpClient, string url,
        T postData, string token, Dictionary<string, string> headers) where T : class
    {
        var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = await CreateJsonContentAsync(postData).ConfigureAwait(false)
        };

        if (!string.IsNullOrWhiteSpace(token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        foreach (var kv in headers)
            if (!req.Headers.Contains(kv.Key))
                req.Headers.TryAddWithoutValidation(kv.Key, kv.Value);

        return await httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
    }

    public static async Task<HttpResponseMessage> PostJsonAsync<T>(this HttpClient httpClient, string url,
        T? postData, string token, Dictionary<string, string> headers) where T : class
    {
        var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = await CreateJsonContentAsync(postData).ConfigureAwait(false)
        };

        if (!string.IsNullOrWhiteSpace(token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        foreach (var kv in headers)
            if (!req.Headers.Contains(kv.Key))
                req.Headers.TryAddWithoutValidation(kv.Key, kv.Value);

        if (url.StartsWith("https://chatgpt.com/backend-api/codex", StringComparison.OrdinalIgnoreCase))
            req.Headers.Host = "chatgpt.com";

        return await httpClient.SendAsync(req).ConfigureAwait(false);
    }
}