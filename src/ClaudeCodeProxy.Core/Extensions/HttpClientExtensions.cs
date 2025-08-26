using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClaudeCodeProxy.Abstraction;

namespace ClaudeCodeProxy.Core.Extensions;

public static class HttpClientExtensions
{
    public static async Task<HttpResponseMessage> HttpRequestRaw(this HttpClient httpClient, string url,
        object? postData,
        string token)
    {
        HttpRequestMessage req = new(HttpMethod.Post, url);

        if (postData != null)
        {
            if (postData is HttpContent data)
            {
                req.Content = data;
            }
            else
            {
                string jsonContent = JsonSerializer.Serialize(postData, ThorJsonSerializer.DefaultOptions);
                var stringContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                req.Content = stringContent;
            }
        }

        if (!string.IsNullOrEmpty(token))
        {
            req.Headers.Add("Authorization", $"Bearer {token}");
        }

        var response = await httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);

        return response;
    }

    public static async Task<HttpResponseMessage> HttpRequestRaw(this HttpClient httpClient, string url,
        object? postData,
        string token, string tokenKey)
    {
        HttpRequestMessage req = new(HttpMethod.Post, url);

        if (postData != null)
        {
            if (postData is HttpContent data)
            {
                req.Content = data;
            }
            else
            {
                string jsonContent = JsonSerializer.Serialize(postData, ThorJsonSerializer.DefaultOptions);
                var stringContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                req.Content = stringContent;
            }
        }

        if (!string.IsNullOrEmpty(token))
        {
            req.Headers.Add(tokenKey, token);
        }


        var response = await httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);

        return response;
    }

    public static async Task<HttpResponseMessage> HttpRequestRaw(this HttpClient httpClient, string url,
        object? postData,
        string token, Dictionary<string, string> headers)
    {
        HttpRequestMessage req = new(HttpMethod.Post, url);

        if (postData != null)
        {
            if (postData is HttpContent data)
            {
                req.Content = data;
            }
            else
            {
                string jsonContent = JsonSerializer.Serialize(postData, ThorJsonSerializer.DefaultOptions);
                var stringContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                req.Content = stringContent;
            }
        }

        if (!string.IsNullOrEmpty(token))
        {
            req.Headers.Add("Authorization", $"Bearer {token}");
        }

        foreach (var header in headers.Where(header => !req.Headers.Contains(header.Key)))
        {
            req.Headers.Add(header.Key, header.Value);
        }


        var response = await httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);

        return response;
    }

    public static async Task<HttpResponseMessage> HttpRequestRaw(this HttpClient httpClient, HttpRequestMessage req,
        object? postData)
    {
        if (postData != null)
        {
            if (postData is HttpContent data)
            {
                req.Content = data;
            }
            else
            {
                string jsonContent = JsonSerializer.Serialize(postData, ThorJsonSerializer.DefaultOptions);
                var stringContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                req.Content = stringContent;
            }
        }

        var response = await httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);

        return response;
    }

    public static async Task<HttpResponseMessage> PostJsonAsync(this HttpClient httpClient, string url,
        object? postData,
        string token)
    {
        HttpRequestMessage req = new(HttpMethod.Post, url);

        if (postData != null)
        {
            if (postData is HttpContent data)
            {
                req.Content = data;
            }
            else
            {
                var stringContent =
                    new StringContent(JsonSerializer.Serialize(postData, ThorJsonSerializer.DefaultOptions),
                        Encoding.UTF8, "application/json");
                req.Content = stringContent;
            }
        }

        if (!string.IsNullOrEmpty(token))
        {
            req.Headers.Add("Authorization", $"Bearer {token}");
        }

        return await httpClient.SendAsync(req);
    }

    public static async Task<HttpResponseMessage> PostJsonAsync(this HttpClient httpClient, string url,
        object? postData,
        string token, Dictionary<string, string> headers)
    {
        HttpRequestMessage req = new(HttpMethod.Post, url);

        if (postData != null)
        {
            if (postData is HttpContent data)
            {
                req.Content = data;
            }
            else
            {
                string jsonContent = JsonSerializer.Serialize(postData, ThorJsonSerializer.DefaultOptions);
                var stringContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                req.Content = stringContent;
            }
        }

        if (!string.IsNullOrEmpty(token))
        {
            req.Headers.Add("Authorization", $"Bearer {token}");
        }

        foreach (var header in headers.Where(header => !req.Headers.Contains(header.Key)))
        {
            req.Headers.Add(header.Key, header.Value);
        }
        
        req.Headers.Add("Host","chatgpt.com");

        return await httpClient.SendAsync(req);
    }

    public static Task<HttpResponseMessage> PostJsonAsync(this HttpClient httpClient, string url, object? postData,
        string token, string tokenKey)
    {
        HttpRequestMessage req = new(HttpMethod.Post, url);

        if (postData != null)
        {
            if (postData is HttpContent data)
            {
                req.Content = data;
            }
            else
            {
                string jsonContent = JsonSerializer.Serialize(postData, ThorJsonSerializer.DefaultOptions);
                var stringContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                req.Content = stringContent;
            }
        }

        if (!string.IsNullOrEmpty(token))
        {
            req.Headers.Add(tokenKey, token);
        }

        return httpClient.SendAsync(req);
    }
}