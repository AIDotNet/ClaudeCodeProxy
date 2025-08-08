using ClaudeCodeProxy.Abstraction;
using ClaudeCodeProxy.Abstraction.Chats;
using ClaudeCodeProxy.Core;
using ClaudeCodeProxy.Core.AI;
using ClaudeCodeProxy.Domain;
using ClaudeCodeProxy.Host.Env;
using ClaudeCodeProxy.Host.Extensions;
using ClaudeCodeProxy.Host.Helper;
using Making.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Thor.Abstractions;
using Thor.Abstractions.Anthropic;

namespace ClaudeCodeProxy.Host.Services;

[MiniApi(Route = "/v1/messages", Tags = "Messages")]
public partial class MessageService(
    AccountsService accountsService,
    SessionHelper sessionHelper)
{
    public async Task HandleAsync(
        HttpContext httpContext,
        [FromServices] ApiKeyService keyService,
        [FromServices] RequestLogService requestLogService,
        [FromServices] WalletService walletService,
        [FromBody] AnthropicInput request,
        [FromServices] IContext context,
        [FromServices] IAnthropicChatCompletionsService chatCompletionsService)
    {
        var apiKey = httpContext.Request.Headers["x-api-key"].FirstOrDefault() ??
                     httpContext.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", string.Empty);

        if (string.IsNullOrEmpty(apiKey))
        {
            httpContext.Response.StatusCode = 401; // Unauthorized
            await httpContext.Response.WriteAsync("Unauthorized API Key",
                cancellationToken: httpContext.RequestAborted);
            return;
        }

        var apiKeyValue = await keyService.GetApiKeyWithRefreshedUsageAsync(apiKey, httpContext.RequestAborted);

        if (apiKeyValue == null)
        {
            httpContext.Response.StatusCode = 401; // Unauthorized
            await httpContext.Response.WriteAsJsonAsync(new
            {
                message = "API Key不存在或已被禁用",
                code = "401"
            }, cancellationToken: httpContext.RequestAborted);
            return;
        }

        if (!apiKeyValue.IsValid())
        {
            httpContext.Response.StatusCode = 403; // Forbidden
            await httpContext.Response.WriteAsJsonAsync(new
            {
                message = "Unauthorized",
                code = "403"
            }, cancellationToken: httpContext.RequestAborted);
            return;
        }

        if (!apiKeyValue.CanAccessService("claude"))
        {
            httpContext.Response.StatusCode = 403; // Forbidden
            await httpContext.Response.WriteAsJsonAsync(new
            {
                message = "当前API Key没有访问Claude服务的权限",
                code = "403"
            }, cancellationToken: httpContext.RequestAborted);
            return;
        }

        if (!apiKeyValue.CanUseModel(request.Model))
        {
            httpContext.Response.StatusCode = 403; // Forbidden
            await httpContext.Response.WriteAsJsonAsync(new
            {
                message = "当前API Key没有使用该模型的权限",
                code = "403"
            }, cancellationToken: httpContext.RequestAborted);
            return;
        }

        var modelPricing = context.ModelPricings
            .FirstOrDefault(p => p.Model == request.Model);

        if (modelPricing is { IsEnabled: false })
        {
            httpContext.Response.StatusCode = 403; // Forbidden
            await httpContext.Response.WriteAsJsonAsync(new
            {
                message = $"模型 {request.Model} 已被管理员禁用",
                code = "403"
            }, cancellationToken: httpContext.RequestAborted);
            return;
        }

        // 获取用户信息
        var userId = apiKeyValue.UserId;
        var userName = apiKeyValue.User?.Username ?? "Unknown";

        // 预估请求费用
        var estimatedCost = EstimateRequestCost(request, httpContext);

        // 获取用户当前余额信息
        var walletDto = await walletService.GetOrCreateWalletAsync(userId);

        // 检查用户钱包余额（使用预估费用）
        var hasSufficientBalance = await walletService.CheckSufficientBalanceAsync(userId, estimatedCost);
        if (!hasSufficientBalance)
        {
            httpContext.Response.StatusCode = 402; // Payment Required
            await httpContext.Response.WriteAsJsonAsync(new
            {
                error = new
                {
                    message = $"钱包余额不足，请充值后重试。当前余额: ${walletDto.Balance:F4}, 预估费用: ${estimatedCost:F4}",
                    type = "insufficient_balance",
                    code = "402",
                    details = new
                    {
                        current_balance = walletDto.Balance,
                        estimated_cost = estimatedCost,
                        currency = "USD"
                    }
                }
            }, cancellationToken: httpContext.RequestAborted);
            return;
        }

        // 检查费用限制
        var costLimitType = apiKeyValue.CheckCostLimit();
        if (costLimitType != null)
        {
            var limitMessage = costLimitType switch
            {
                "daily" => "API Key已达到每日费用限制",
                "monthly" => "API Key已达到月度费用限制",
                "total" => "API Key已达到总费用限制",
                _ => "API Key已达到费用限制"
            };

            httpContext.Response.StatusCode = 429; // Too Many Requests
            await httpContext.Response.WriteAsJsonAsync(new
            {
                error = new
                {
                    message = limitMessage,
                    type = "rate_limit_error",
                    code = "429"
                }
            }, cancellationToken: httpContext.RequestAborted);
            return;
        }

        if (!string.IsNullOrEmpty(apiKeyValue.Model))
        {
            request.Model = apiKeyValue.Model;
        }

        var sessionHash = sessionHelper.GenerateSessionHash(request);

        var account =
            await accountsService.SelectAccountForApiKey(apiKeyValue, sessionHash, request.Model,
                httpContext.RequestAborted);

        // 实现模型映射功能
        var mappedModel = MapRequestedModel(request.Model, account);
        if (!string.IsNullOrEmpty(mappedModel) && mappedModel != request.Model)
        {
            // 记录模型映射日志
            var logger = httpContext.RequestServices.GetRequiredService<ILogger<MessageService>>();
            logger.LogInformation("🔄 模型映射: {OriginalModel} -> {MappedModel} for account {AccountName}",
                request.Model, mappedModel, account?.Name);
            request.Model = mappedModel;
        }

        // 创建请求日志
        var requestStartTime = DateTime.Now;
        var requestLog = await requestLogService.CreateRequestLogAsync(
            userId,
            apiKeyValue.Id,
            apiKeyValue.Name,
            request.Model,
            requestStartTime,
            "claude",
            httpContext.Connection.RemoteIpAddress?.ToString(),
            httpContext.Request.Headers["User-Agent"].FirstOrDefault(),
            Guid.NewGuid().ToString(),
            account?.Id,
            account?.Name,
            request.Stream,
            new Dictionary<string, object>
            {
                ["user_id"] = userId,
                ["user_name"] = userName,
                ["api_key_name"] = apiKeyValue.Name
            },
            httpContext.RequestAborted);

        // 寻找对应的账号
        if (account is { IsClaude: true })
        {
            await HandleClaudeAsync(httpContext, request, chatCompletionsService, apiKeyValue,
                account, requestLog.Id, requestLogService,
                httpContext.RequestAborted);
        }
        else if (account is { IsClaudeConsole: true })
        {
            await HandleClaudeAsync(httpContext, request, chatCompletionsService, apiKeyValue,
                account, requestLog.Id, requestLogService,
                httpContext.RequestAborted);
        }
        else if (account?.IsOpenAI == true)
        {
            await HandleOpenAIAsync(httpContext, request, apiKeyValue, account, requestLog.Id, requestLogService,
                httpContext.RequestAborted);
        }
        else
        {
            // 如果没有找到对应的账号，返回403 Forbidden
            httpContext.Response.StatusCode = 403; // Forbidden
            await httpContext.Response.WriteAsJsonAsync(new
            {
                message = "当前API Key没有访问Claude服务的权限",
                code = "403"
            }, cancellationToken: httpContext.RequestAborted);
        }
    }

    private async Task HandleOpenAIAsync(
        HttpContext httpContext,
        AnthropicInput request,
        ApiKey apiKeyValue,
        Accounts? account,
        Guid requestLogId,
        RequestLogService requestLogService,
        CancellationToken cancellationToken = default)
    {
        var chatCompletionsService =
            httpContext.RequestServices.GetRequiredService<OpenAIAnthropicChatCompletionsService>();

        var accessToken = await accountsService.GetValidAccessTokenAsync(account, cancellationToken);

        try
        {
            // 准备请求头和代理配置
            var headers = new Dictionary<string, string>()
            {
                { "Authorization", "Bearer " + accessToken },
                { "anthropic-version", EnvHelper.ApiVersion }
            };

            // 复制context的请求头
            // foreach (var header in httpContext.Request.Headers)
            // {
            //     // 不要覆盖已有的Authorization和Content-Type头
            //     if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase) ||
            //         header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) ||
            //         header.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase) ||
            //         header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
            //     {
            //         continue;
            //     }
            //
            //     headers[header.Key] = header.Value.ToString();
            // }

            headers["anthropic-beta"] = EnvHelper.BetaHeader;

            var proxyConfig = account?.Proxy;

            // 调用真实的聊天完成服务
            ClaudeChatCompletionDto response;
            // 从response中提取实际的token usage信息
            var inputTokens = 0;
            var outputTokens = 0;
            var cacheCreateTokens = 0;
            var cacheReadTokens = 0;

            if (request.Stream)
            {
                // 是否第一次输出
                bool isFirst = true;

                await foreach (var (eventName, value, item) in chatCompletionsService.StreamChatCompletionsAsync(
                                   request,
                                   headers, proxyConfig, new ThorPlatformOptions()
                                   {
                                       Address = account?.ApiUrl ?? "https://api.anthropic.com/",
                                   }, cancellationToken))
                {
                    if (isFirst)
                    {
                        httpContext.SetEventStreamHeaders();
                        isFirst = false;
                    }

                    if (item?.Usage is { input_tokens: > 0 } ||
                        item?.message?.Usage?.input_tokens > 0)
                    {
                        inputTokens = item.Usage?.input_tokens ?? item?.message?.Usage?.input_tokens ?? 0;
                    }

                    if (item?.Usage is { output_tokens: > 0 } || item?.message?.Usage?.output_tokens > 0)
                    {
                        outputTokens = (item.Usage?.output_tokens ?? item?.message?.Usage?.output_tokens) ?? 0;
                    }

                    if (item?.Usage is { cache_creation_input_tokens: > 0 } ||
                        item?.message?.Usage?.cache_creation_input_tokens > 0)
                    {
                        cacheCreateTokens += item.Usage?.cache_creation_input_tokens ??
                                             item?.message?.Usage?.cache_creation_input_tokens ?? 0;
                    }

                    if (item?.Usage is { cache_read_input_tokens: > 0 } ||
                        item?.message?.Usage?.cache_read_input_tokens > 0)
                    {
                        cacheReadTokens += item.Usage?.cache_read_input_tokens ??
                                           item.message?.Usage?.cache_read_input_tokens ?? 0;
                    }


                    await httpContext.WriteAsEventStreamDataAsync(eventName, value);
                }
            }

            else
            {
                // 非流式响应
                response = await chatCompletionsService.ChatCompletionsAsync(request, headers, proxyConfig,
                    new ThorPlatformOptions()
                    {
                        Address = account.ApiUrl,
                    },
                    cancellationToken);

                // 从非流式响应中提取Usage信息
                if (response?.Usage != null)
                {
                    inputTokens = response.Usage.input_tokens ?? 0;
                    outputTokens = response.Usage.output_tokens ?? 0;
                    cacheCreateTokens = response.Usage.cache_creation_input_tokens ?? 0;
                    cacheReadTokens = response.Usage.cache_read_input_tokens ?? 0;

                    // 记录Usage提取日志
                    var logger = httpContext.RequestServices.GetRequiredService<ILogger<MessageService>>();
                    logger.LogDebug(
                        "非流式响应Usage提取: Input={InputTokens}, Output={OutputTokens}, CacheCreate={CacheCreate}, CacheRead={CacheRead}",
                        inputTokens, outputTokens, cacheCreateTokens, cacheReadTokens);
                }

                await httpContext.Response.WriteAsJsonAsync(response, cancellationToken: cancellationToken);
            }

            // 计算费用（这里需要根据实际的定价模型来计算）
            var cost = CalculateTokenCost(request.Model, inputTokens, outputTokens, cacheCreateTokens,
                cacheReadTokens, httpContext);

            // 注意：我们不能直接修改实体然后保存，需要通过服务方法来更新
            // 这里只是增加使用计数，具体的更新逻辑应该在服务层处理
            // 可以考虑创建专门的方法来更新使用统计

            // 完成请求日志记录（成功）
            await requestLogService.CompleteRequestLogAsync(
                requestLogId,
                inputTokens: inputTokens,
                outputTokens: outputTokens,
                cacheCreateTokens: cacheCreateTokens,
                cacheReadTokens: cacheReadTokens,
                cost: cost,
                status: "success",
                httpStatusCode: 200,
                cancellationToken: cancellationToken);
        }
        catch (RateLimitException rateLimitEx)
        {
            // 处理限流异常 - 自动设置账户限流状态
            if (account != null)
            {
                var rateLimitedUntil = rateLimitEx.RateLimitInfo.RateLimitedUntil;
                await accountsService.SetRateLimitAsync(account.Id, rateLimitedUntil, rateLimitEx.Message,
                    cancellationToken);

                // 记录限流日志
                var logger = httpContext.RequestServices.GetRequiredService<ILogger<MessageService>>();
                logger.LogWarning("账户 {AccountName} (ID: {AccountId}) 达到限流，限流解除时间：{RateLimitedUntil}",
                    account.Name, account.Id, rateLimitedUntil);
            }

            // 完成请求日志记录（限流失败）
            await requestLogService.CompleteRequestLogAsync(
                requestLogId,
                status: "rate_limited",
                errorMessage: rateLimitEx.Message,
                httpStatusCode: 429,
                cancellationToken: cancellationToken);

            // 返回429限流错误
            httpContext.Response.StatusCode = 429;
            httpContext.Response.Headers["Retry-After"] = rateLimitEx.RateLimitInfo.RetryAfterSeconds.ToString();

            await httpContext.Response.WriteAsJsonAsync(new
            {
                error = new
                {
                    message = rateLimitEx.Message,
                    type = "rate_limit_error",
                    code = "429"
                }
            }, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            // 完成请求日志记录（失败）
            await requestLogService.CompleteRequestLogAsync(
                requestLogId,
                status: "error",
                errorMessage: ex.Message,
                httpStatusCode: 500,
                cancellationToken: cancellationToken);

            httpContext.Response.StatusCode = 500;
            await httpContext.Response.WriteAsJsonAsync(new
            {
                // 返回claude需要的异常
                error = new
                {
                    message = ex.Message,
                    type = "server_error",
                    code = "500"
                }
            }, cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// 处理系统提示词 - 如果不是真实的Claude Code请求，需要添加Claude Code系统提示词
    /// </summary>
    /// <param name="request">AnthropicInput请求</param>
    /// <param name="httpContext">HTTP上下文</param>
    private void ProcessSystemPrompts(AnthropicInput request, HttpContext httpContext)
    {
        // 判断是否是真实的Claude Code请求
        var isRealClaudeCode = IsClaudeCodeRequest(httpContext);

        // 如果不是真实的Claude Code请求，需要设置Claude Code系统提示词
        if (!isRealClaudeCode)
        {
            var claudeCodePrompt = new AnthropicMessageContent
            {
                Type = "text",
                Text = PromptConstant.ClaudeCodeSystemPrompt,
                CacheControl = new AnthropicCacheControl { Type = "ephemeral" }
            };

            if (request.SystemCalculated != null)
            {
                if (request.System != null)
                {
                    // 字符串格式：转换为数组，Claude Code 提示词在第一位
                    var userSystemPrompt = new AnthropicMessageContent
                    {
                        Type = "text",
                        Text = request.System
                    };

                    // 如果用户的提示词与Claude Code提示词相同，只保留一个
                    if (request.System.Trim() == PromptConstant.ClaudeCodeSystemPrompt)
                    {
                        request.Systems = new List<AnthropicMessageContent> { claudeCodePrompt };
                    }
                    else
                    {
                        request.Systems = new List<AnthropicMessageContent> { claudeCodePrompt, userSystemPrompt };
                    }

                    request.System = null;
                }
                else if (request.Systems != null)
                {
                    // 检查第一个元素是否是Claude Code系统提示词
                    var firstItem = request.Systems.FirstOrDefault();
                    var isFirstItemClaudeCode = firstItem != null &&
                                                firstItem.Type == "text" &&
                                                firstItem.Text == PromptConstant.ClaudeCodeSystemPrompt;

                    if (!isFirstItemClaudeCode)
                    {
                        // 如果第一个不是Claude Code提示词，需要在开头插入
                        // 同时检查数组中是否有其他位置包含Claude Code提示词，如果有则移除
                        var filteredSystem = request.Systems
                            .Where(item =>
                                !(item != null && item.Type == "text" &&
                                  item.Text == PromptConstant.ClaudeCodeSystemPrompt))
                            .ToList();

                        var newSystems = new List<AnthropicMessageContent> { claudeCodePrompt };
                        newSystems.AddRange(filteredSystem);
                        request.Systems = newSystems;
                    }
                }
            }
            else
            {
                // 用户没有传递system，需要添加Claude Code提示词
                request.Systems = new List<AnthropicMessageContent> { claudeCodePrompt };
            }

            request.System = null;

            request.Systems.Insert(0, new AnthropicMessageContent
            {
                Type = "text",
                Text = PromptConstant.ClaudeCliSystemPrompt,
                CacheControl = new AnthropicCacheControl { Type = "ephemeral" }
            });
        }
    }

    /// <summary>
    /// 处理Claude请求
    /// </summary>
    private async Task HandleClaudeAsync(
        HttpContext httpContext,
        AnthropicInput request,
        IAnthropicChatCompletionsService chatCompletionsService,
        ApiKey apiKeyValue,
        Accounts? account,
        Guid requestLogId,
        RequestLogService requestLogService,
        CancellationToken cancellationToken = default)
    {
        // 注意：请求日志已在主方法中创建，这里直接使用传入的requestLogId

        var accessToken = await accountsService.GetValidAccessTokenAsync(account, cancellationToken);

        // 处理系统提示词 - 如果不是真实的Claude Code请求，需要添加Claude Code系统提示词
        ProcessSystemPrompts(request, httpContext);

        try
        {
            // 准备请求头和代理配置
            var headers = new Dictionary<string, string>()
            {
                { "Authorization", "Bearer " + accessToken },
                { "anthropic-version", EnvHelper.ApiVersion }
            };

            if (IsClaudeCodeRequest(httpContext))
            {
                // 复制context的请求头
                foreach (var header in httpContext.Request.Headers)
                {
                    // 不要覆盖已有的Authorization和Content-Type头
                    if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase) ||
                        header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) ||
                        header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) ||
                        header.Key.Equals("x-api-key", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    headers[header.Key] = header.Value.ToString();
                }
            }
            else
            {
                headers["User-Agent"] = "claude-cli/1.0.67 (external,cli)";
                headers["Accept-Encoding"] = "gzip, deflate, br";
                headers["Accept-Language"] = "*";
                headers["X-Stainless-Retry-Count"] = "0";
                headers["x-Stainless-Timeout"] = "60";
                headers["X-Stainless-Lang"] = "js";
                headers["X-Stainless-Package-Version"] = "0.55.1";
                headers["X-Stainless-OS"] = "Windows";
                headers["X-Stainless-Arch"] = "x64";
                headers["X-Stainless-Runtime"] = "node";
                headers["X-Stainless-Runtime-Version"] = "v22.15.0";
                headers["anthropic-dangerous-direct-browser-access"] = "true";
                headers["x-app"] = "cli";
                headers["sec-fetch-mode"] = "cors";
                headers["sec-fetch-site"] = "cross-site";
                headers["sec-fetch-dest"] = "empty";
            }


            headers["anthropic-beta"] = EnvHelper.BetaHeader;

            var proxyConfig = account?.Proxy;

            // 调用真实的聊天完成服务
            ClaudeChatCompletionDto response;
            // 从response中提取实际的token usage信息
            var inputTokens = 0;
            var outputTokens = 0;
            var cacheCreateTokens = 0;
            var cacheReadTokens = 0;

            if (request.Stream)
            {
                // 是否第一次输出
                bool isFirst = true;

                await foreach (var (eventName, value, item) in chatCompletionsService.StreamChatCompletionsAsync(
                                   request,
                                   headers, proxyConfig, new ThorPlatformOptions()
                                   {
                                       Address = account?.ApiUrl ?? "https://api.anthropic.com/",
                                   }, cancellationToken))
                {
                    if (isFirst)
                    {
                        httpContext.SetEventStreamHeaders();
                        isFirst = false;
                    }

                    if (item?.Usage is { input_tokens: > 0 } ||
                        item?.message?.Usage?.input_tokens > 0)
                    {
                        inputTokens = item.Usage?.input_tokens ?? item?.message?.Usage?.input_tokens ?? 0;
                    }

                    if (item?.Usage is { output_tokens: > 0 } || item?.message?.Usage?.output_tokens > 0)
                    {
                        outputTokens = (item.Usage?.output_tokens ?? item?.message?.Usage?.output_tokens) ?? 0;
                    }

                    if (item?.Usage is { cache_creation_input_tokens: > 0 } ||
                        item?.message?.Usage?.cache_creation_input_tokens > 0)
                    {
                        cacheCreateTokens += item.Usage?.cache_creation_input_tokens ??
                                             item?.message?.Usage?.cache_creation_input_tokens ?? 0;
                    }

                    if (item?.Usage is { cache_read_input_tokens: > 0 } ||
                        item?.message?.Usage?.cache_read_input_tokens > 0)
                    {
                        cacheReadTokens += item.Usage?.cache_read_input_tokens ??
                                           item.message?.Usage?.cache_read_input_tokens ?? 0;
                    }


                    await httpContext.WriteAsEventStreamDataAsync(eventName, value);
                }
            }

            else
            {
                // 非流式响应
                response = await chatCompletionsService.ChatCompletionsAsync(request, headers, proxyConfig,
                    new ThorPlatformOptions()
                    {
                        Address = account.ApiUrl,
                    },
                    cancellationToken);

                // 从非流式响应中提取Usage信息
                if (response?.Usage != null)
                {
                    inputTokens = response.Usage.input_tokens ?? 0;
                    outputTokens = response.Usage.output_tokens ?? 0;
                    cacheCreateTokens = response.Usage.cache_creation_input_tokens ?? 0;
                    cacheReadTokens = response.Usage.cache_read_input_tokens ?? 0;

                    // 记录Usage提取日志
                    var logger = httpContext.RequestServices.GetRequiredService<ILogger<MessageService>>();
                    logger.LogDebug(
                        "非流式响应Usage提取: Input={InputTokens}, Output={OutputTokens}, CacheCreate={CacheCreate}, CacheRead={CacheRead}",
                        inputTokens, outputTokens, cacheCreateTokens, cacheReadTokens);
                }

                await httpContext.Response.WriteAsJsonAsync(response, cancellationToken: cancellationToken);
            }

            // 计算费用（这里需要根据实际的定价模型来计算）
            var cost = CalculateTokenCost(request.Model, inputTokens, outputTokens, cacheCreateTokens,
                cacheReadTokens, httpContext);

            // 注意：我们不能直接修改实体然后保存，需要通过服务方法来更新
            // 这里只是增加使用计数，具体的更新逻辑应该在服务层处理
            // 可以考虑创建专门的方法来更新使用统计

            // 完成请求日志记录（成功）
            await requestLogService.CompleteRequestLogAsync(
                requestLogId,
                inputTokens: inputTokens,
                outputTokens: outputTokens,
                cacheCreateTokens: cacheCreateTokens,
                cacheReadTokens: cacheReadTokens,
                cost: cost,
                status: "success",
                httpStatusCode: 200,
                cancellationToken: cancellationToken);
        }
        catch (RateLimitException rateLimitEx)
        {
            // 处理限流异常 - 自动设置账户限流状态
            if (account != null)
            {
                var rateLimitedUntil = rateLimitEx.RateLimitInfo.RateLimitedUntil;
                await accountsService.SetRateLimitAsync(account.Id, rateLimitedUntil, rateLimitEx.Message,
                    cancellationToken);

                // 记录限流日志
                var logger = httpContext.RequestServices.GetRequiredService<ILogger<MessageService>>();
                logger.LogWarning("账户 {AccountName} (ID: {AccountId}) 达到限流，限流解除时间：{RateLimitedUntil}",
                    account.Name, account.Id, rateLimitedUntil);
            }

            // 完成请求日志记录（限流失败）
            await requestLogService.CompleteRequestLogAsync(
                requestLogId,
                status: "rate_limited",
                errorMessage: rateLimitEx.Message,
                httpStatusCode: 429,
                cancellationToken: cancellationToken);

            // 返回429限流错误
            httpContext.Response.StatusCode = 429;
            httpContext.Response.Headers["Retry-After"] = rateLimitEx.RateLimitInfo.RetryAfterSeconds.ToString();

            await httpContext.Response.WriteAsJsonAsync(new
            {
                error = new
                {
                    message = rateLimitEx.Message,
                    type = "rate_limit_error",
                    code = "429"
                }
            }, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            // 完成请求日志记录（失败）
            await requestLogService.CompleteRequestLogAsync(
                requestLogId,
                status: "error",
                errorMessage: ex.Message,
                httpStatusCode: 500,
                cancellationToken: cancellationToken);

            httpContext.Response.StatusCode = 500;
            await httpContext.Response.WriteAsJsonAsync(new
            {
                // 返回claude需要的异常
                error = new
                {
                    message = ex.Message,
                    type = "server_error",
                    code = "500"
                }
            }, cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// 计算Token费用
    /// </summary>
    private decimal CalculateTokenCost(string model, int inputTokens, int outputTokens,
        int cacheCreateTokens, int cacheReadTokens, HttpContext httpContext)
    {
        // 获取价格服务
        var pricingService = httpContext.RequestServices.GetRequiredService<PricingService>();

        // 计算费用
        var cost = pricingService.CalculateTokenCost(
            model, inputTokens, outputTokens, cacheCreateTokens, cacheReadTokens);

        // 记录费用计算日志
        var logger = httpContext.RequestServices.GetRequiredService<ILogger<MessageService>>();
        logger.LogInformation("费用计算结果: 模型={Model}, 总计=${TotalCost:F6}", model, cost);

        return cost;
    }

    /// <summary>
    /// 预估请求费用（基于输入内容的粗略估算）
    /// </summary>
    private decimal EstimateRequestCost(AnthropicInput request, HttpContext httpContext)
    {
        try
        {
            // 粗略估算输入token数量（按字符数 / 4 估算，这是一个简化的方法）
            var estimatedInputTokens = 0;

            if (request.Messages != null)
            {
                foreach (var message in request.Messages)
                {
                    if (message.Content is string textContent)
                    {
                        estimatedInputTokens += textContent.Length / 4; // 粗略估算
                    }
                    else if (message.Content is not string && message.Content is not null)
                    {
                        // 假设是对象数组，尝试转换为字符串计算
                        var contentString = message.Content.ToString();
                        if (!string.IsNullOrEmpty(contentString))
                        {
                            estimatedInputTokens += contentString.Length / 4;
                        }
                    }
                }
            }

            // 估算输出token数量（按最大输出的30%估算，避免过高预估）
            var maxTokens = request.MaxTokens ?? 4096;
            var estimatedOutputTokens = Math.Min(maxTokens * 0.3m, 1000); // 最多按1000个输出token估算

            // 使用PricingService计算费用
            var pricingService = httpContext.RequestServices.GetRequiredService<PricingService>();
            var estimatedCost = pricingService.CalculateTokenCost(
                request.Model,
                estimatedInputTokens,
                (int)estimatedOutputTokens);

            // 添加20%的安全余量
            return estimatedCost * 1.2m;
        }
        catch (Exception)
        {
            // 如果估算失败，返回一个保守的估算值
            return 0.1m; // 0.1美元作为默认预估
        }
    }

    /// <summary>
    /// 判断当前请求是否Claude Code发起，如果是则需要提供默认的请求头
    /// </summary>
    /// <returns></returns>
    private bool IsClaudeCodeRequest(HttpContext httpContext)
    {
        // 检查是否有特定的请求头或标识符来判断是否是Claude Code发起的请求
        // 这里可以根据实际情况调整
        return httpContext.Request.Headers.UserAgent.ToString()
            .Contains("claude-cli", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 根据账户配置映射请求的模型
    /// </summary>
    /// <param name="requestedModel">请求的原始模型</param>
    /// <param name="account">使用的账户</param>
    /// <returns>映射后的模型名称，如果没有映射则返回原始模型</returns>
    private string MapRequestedModel(string requestedModel, Accounts? account)
    {
        // 如果账户为空或没有配置模型映射，返回原始模型
        if (account?.SupportedModels == null || account.SupportedModels.Count == 0)
        {
            return requestedModel;
        }

        try
        {
            // 查找模型映射：格式为 "sourceModel:targetModel"
            foreach (var mapping in account.SupportedModels)
            {
                var parts = mapping.Split(':', 2);
                if (parts.Length == 2)
                {
                    var sourceModel = parts[0].Trim();
                    var targetModel = parts[1].Trim();

                    // 如果找到匹配的源模型，返回目标模型
                    if (string.Equals(sourceModel, requestedModel, StringComparison.OrdinalIgnoreCase))
                    {
                        return targetModel;
                    }
                }
            }

            // 如果没有找到映射，返回原始模型
            return requestedModel;
        }
        catch
        {
            // 解析失败时，返回原始模型
            return requestedModel;
        }
    }
}