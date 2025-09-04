using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClaudeCodeProxy.Abstraction;
using ClaudeCodeProxy.Core.Extensions;
using ClaudeCodeProxy.Domain;
using Microsoft.Extensions.Logging;
using Thor.Abstractions;
using Thor.Abstractions.Chats;
using Thor.Abstractions.Chats.Dtos;
using Thor.Abstractions.Exceptions;

namespace ClaudeCodeProxy.Core.AI;

public sealed class OpenAIChatCompletionsService(ILogger<OpenAIChatCompletionsService> logger)
    : IThorChatCompletionsService
{
    public async Task<ThorChatCompletionsResponse> ChatCompletionsAsync(ThorChatCompletionsRequest chatCompletionCreate,
        Dictionary<string, string> headers,
        ProxyConfig? config,
        ThorPlatformOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using var openai =
            Activity.Current?.Source.StartActivity("OpenAI 对话补全");

        // 判断是否是魔塔
        if (options?.Address.StartsWith("https://api-inference.modelscope.cn") == false)
        {
            chatCompletionCreate.StreamOptions = new ThorStreamOptions()
            {
                IncludeUsage = true
            };
        }

        if (string.IsNullOrEmpty(options?.Address))
        {
            options.Address = "https://api.openai.com/v1";
        }

        var response = await HttpClientFactory.GetHttpClient(options.Address, config).PostJsonAsync(
            options?.Address.TrimEnd('/') + "/chat/completions",
            chatCompletionCreate, options.ApiKey, headers).ConfigureAwait(false);

        openai?.SetTag("Model", chatCompletionCreate.Model);
        openai?.SetTag("Response", response.StatusCode.ToString());

        switch (response.StatusCode)
        {
            case HttpStatusCode.Unauthorized:
                throw new BusinessException("渠道未登录,请联系管理人员", "401");
            // 大于等于400的状态码都认为是异常
            case >= HttpStatusCode.BadRequest:
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                logger.LogError("OpenAI对话异常 请求地址：{Address}, StatusCode: {StatusCode} Response: {Response}", options.Address,
                    response.StatusCode, error);

                throw new BusinessException("OpenAI对话异常:" + error, response.StatusCode.ToString());
            }
            default:
            {
                var result =
                    await response.Content.ReadFromJsonAsync<ThorChatCompletionsResponse>(
                        cancellationToken: cancellationToken).ConfigureAwait(false);

                return result;
            }
        }
    }

    public async IAsyncEnumerable<ThorChatCompletionsResponse> StreamChatCompletionsAsync(
        ThorChatCompletionsRequest chatCompletionCreate,
        Dictionary<string, string> headers,
        ProxyConfig? config,
        ThorPlatformOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using var openai =
            Activity.Current?.Source.StartActivity("OpenAI 对话流式补全");

        // 判断是否是魔塔
        if (options?.Address.StartsWith("https://api-inference.modelscope.cn") == false)
        {
            chatCompletionCreate.StreamOptions = new ThorStreamOptions()
            {
                IncludeUsage = true
            };
        }


        var response = await HttpClientFactory.GetHttpClient(options.Address, config).HttpRequestRaw(
            options?.Address.TrimEnd('/') + "/chat/completions",
            chatCompletionCreate, options.ApiKey, headers);

        openai?.SetTag("Model", chatCompletionCreate.Model);
        openai?.SetTag("Response", response.StatusCode.ToString());

        // 大于等于400的状态码都认为是异常
        if (response.StatusCode >= HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("OpenAI对话异常 , StatusCode: {StatusCode} 错误响应内容：{Content}", response.StatusCode,
                error);

            throw new BusinessException("OpenAI对话异常：" + error, response.StatusCode.ToString());
        }

        using var stream = new StreamReader(await response.Content.ReadAsStreamAsync(cancellationToken));

        using StreamReader reader = new(await response.Content.ReadAsStreamAsync(cancellationToken));
        string? line = string.Empty;
        while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null)
        {
            line += Environment.NewLine;

            if (line.StartsWith('{'))
            {
                logger.LogInformation("OpenAI对话异常 , StatusCode: {StatusCode} Response: {Response}", response.StatusCode,
                    line);

                throw new BusinessException("OpenAI对话异常" + line, "500");
            }

            if (line.StartsWith(OpenAIConstant.Data))
                line = line[OpenAIConstant.Data.Length..];

            line = line.Trim();

            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line == OpenAIConstant.Done)
            {
                break;
            }

            if (line.StartsWith(':'))
            {
                continue;
            }

            var result = JsonSerializer.Deserialize<ThorChatCompletionsResponse>(line,
                ThorJsonSerializer.DefaultOptions);

            if (result == null)
            {
                continue;
            }

            yield return result;
        }
    }
}