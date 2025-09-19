using ClaudeCodeProxy.Abstraction.Chats;
using ClaudeCodeProxy.Core;
using ClaudeCodeProxy.Core.AI;
using ClaudeCodeProxy.Core.AI.Responses;
using ClaudeCodeProxy.Domain;
using ClaudeCodeProxy.Host.Extensions;
using ClaudeCodeProxy.Host.Helper;
using Making.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Thor.Abstractions;
using Thor.Abstractions.Responses;
using Thor.Abstractions.Responses.Dto;

namespace ClaudeCodeProxy.Host.Services.AI
{
    [MiniApi(Route = "/v1/responses", Tags = "OpenAI")]
    public class OpenAIResponses(
    AccountsService accountsService,
    ILogger<MessageService> logger,
    SessionHelper sessionHelper)
    {
        [HttpPost("/")]
        public async Task HandleAsync(
        HttpContext httpContext,
        [FromServices] ApiKeyService keyService,
        [FromServices] RequestLogService requestLogService,
        [FromServices] WalletService walletService,
        [FromBody] ResponsesInput request,
        [FromServices] IContext context,
        [FromServices] IAnthropicChatCompletionsService chatCompletionsService)
        {
            var apiKey = httpContext.Request.Headers["x-api-key"].FirstOrDefault() ??
                         httpContext.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", string.Empty);

            if (string.IsNullOrEmpty(apiKey))
            {
                httpContext.Response.StatusCode = 401; // Unauthorized
                await httpContext.Response.WriteAsync("Unauthorized API Key",
                    httpContext.RequestAborted);
                return;
            }

            var apiKeyValue = await keyService.GetApiKeyWithRefreshedUsageAsync(apiKey, httpContext.RequestAborted)
                .ConfigureAwait(false);

            if (apiKeyValue == null)
            {
                httpContext.Response.StatusCode = 401; // Unauthorized
                await httpContext.Response.WriteAsJsonAsync(new
                {
                    message = "API Key不存在或已被禁用",
                    code = "401"
                }, httpContext.RequestAborted)
                    .ConfigureAwait(false);
                return;
            }

            if (!apiKeyValue.IsValid())
            {
                httpContext.Response.StatusCode = 403; // Forbidden
                await httpContext.Response.WriteAsJsonAsync(new
                {
                    message = "Unauthorized",
                    code = "403"
                }, httpContext.RequestAborted);
                return;
            }

            if (!apiKeyValue.CanAccessService("claude"))
            {
                httpContext.Response.StatusCode = 403; // Forbidden
                await httpContext.Response.WriteAsJsonAsync(new
                {
                    message = "当前API Key没有访问Claude服务的权限",
                    code = "403"
                }, httpContext.RequestAborted);
                return;
            }

            if (!apiKeyValue.CanUseModel(request.Model))
            {
                httpContext.Response.StatusCode = 403; // Forbidden
                await httpContext.Response.WriteAsJsonAsync(new
                {
                    message = "当前API Key没有使用该模型的权限",
                    code = "403"
                }, httpContext.RequestAborted);
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
                }, httpContext.RequestAborted);
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
                }, httpContext.RequestAborted);
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
                }, httpContext.RequestAborted);
                return;
            }

            if (!string.IsNullOrEmpty(apiKeyValue.Model)) request.Model = apiKeyValue.Model;

            var account =
                await accountsService.SelectAccountForApiKey(apiKeyValue, Guid.NewGuid().ToString(), request.Model,
                    true,
                    httpContext.RequestAborted);

            // 实现模型映射功能
            var mappedModel = MapRequestedModel(request.Model, account);
            if (!string.IsNullOrEmpty(mappedModel) && mappedModel != request.Model)
            {
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
                httpContext.Request.Headers.UserAgent.FirstOrDefault(),
                Guid.NewGuid().ToString(),
                account?.Id,
                account?.Name,
                request.Stream == true,
                new Dictionary<string, object>
                {
                    ["user_id"] = userId,
                    ["user_name"] = userName,
                    ["api_key_name"] = apiKeyValue.Name
                },
                httpContext.RequestAborted);

            // 寻找对应的账号
            //if (account is { IsClaude: true })
            //{
            //    await HandleClaudeAsync(httpContext, request, chatCompletionsService, apiKeyValue,
            //        account, requestLog.Id, requestLogService,
            //        httpContext.RequestAborted);
            //}
            //else if (account is { IsClaudeConsole: true })
            //{
            //    await HandleClaudeAsync(httpContext, request, chatCompletionsService, apiKeyValue,
            //        account, requestLog.Id, requestLogService,
            //        httpContext.RequestAborted);
            //}
            //else 
            if (account?.IsOpenAI == true)
            {
                await HandleOpenAiResponsesAsync(httpContext, request, apiKeyValue, account, requestLog.Id,
                    requestLogService,
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
                }, httpContext.RequestAborted);
            }
        }


        private async Task HandleOpenAiResponsesAsync(
            HttpContext httpContext,
            ResponsesInput request,
            ApiKey apiKeyValue,
            Accounts? account,
            Guid requestLogId,
            RequestLogService requestLogService,
            CancellationToken cancellationToken = default)
        {
            var openAIResponses =
                httpContext.RequestServices.GetRequiredService<IThorResponsesService>();

            var accessToken = await accountsService.GetValidAccessTokenAsync(account, cancellationToken);

            try
            {
                // 准备请求头和代理配置
                var headers = new Dictionary<string, string>
            {
                { "Authorization", "Bearer " + accessToken }
            };

                var proxyConfig = account?.Proxy;

                // 调用OpenAI Responses服务（转换为Claude格式输出）
                ResponsesDto response;
                // 从response中提取实际的token usage信息
                var inputTokens = 0;
                var outputTokens = 0;
                var cacheCreateTokens = 0;
                var cacheReadTokens = 0;

                var options = new ThorPlatformOptions();

                if (string.IsNullOrEmpty(account?.ApiUrl)) options.Address = "https://chatgpt.com/backend-api/codex";

                if (request.Stream == true)
                {
                    // 是否第一次输出
                    var isFirst = true;

                    // 判断当前请求是否包含了Codex的提示词
                    if(string.IsNullOrEmpty(request.Instructions))
                    {
                        request.Instructions = AIPrompt.CodeXPrompt;
                    }
                    request.Store = false;
                    request.ServiceTier = null;

                    await foreach (var (eventName, item) in openAIResponses.GetResponsesAsync(
                                       request,
                                       headers, proxyConfig, options, cancellationToken))
                    {
                        if (isFirst)
                        {
                            httpContext.SetEventStreamHeaders();
                            // 添加配额响应头（流式响应）
                            AddQuotaHeaders(httpContext, apiKeyValue);
                            isFirst = false;
                        }

                        if (item?.Response?.Usage is { InputTokens: > 0 })
                            inputTokens = item.Response.Usage?.InputTokens ?? 0;

                        if (item?.Response?.Usage is { OutputTokens: > 0 })
                            outputTokens = (item.Response.Usage?.OutputTokens) ?? 0;

                        //if (item?.Response.Usage is { CacheCreationInputTokens: > 0 } ||
                        //    item?.Message?.Usage?.CacheCreationInputTokens > 0)
                        //    cacheCreateTokens += item.Usage?.CacheCreationInputTokens ??
                        //                         item?.Message?.Usage?.CacheCreationInputTokens ?? 0;

                        if (item?.Response?.Usage?.InputTokensDetails is { CachedTokens: > 0 })
                            cacheReadTokens += item?.Response?.Usage?.InputTokensDetails?.CachedTokens ?? 0;

                        await httpContext.WriteAsEventStreamDataAsync(eventName, item);
                    }
                }
                else
                {
                    // 非流式响应
                    response = await openAIResponses.GetResponseAsync(request, headers, proxyConfig,
                        options,
                        cancellationToken);

                    // 从非流式响应中提取Usage信息
                    if (response?.Usage != null)
                    {
                        inputTokens = response.Usage.InputTokens;
                        outputTokens = response.Usage.OutputTokens;
                        cacheReadTokens = response.Usage?.InputTokensDetails?.CachedTokens ?? 0;

                        // 记录Usage提取日志
                        var logger = httpContext.RequestServices.GetRequiredService<ILogger<MessageService>>();
                        logger.LogDebug(
                            "OpenAI Responses非流式响应Usage提取: Input={InputTokens}, Output={OutputTokens}, CacheCreate={CacheCreate}, CacheRead={CacheRead}",
                            inputTokens, outputTokens, cacheCreateTokens, cacheReadTokens);
                    }

                    await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
                }

                // 计算费用（这里需要根据实际的定价模型来计算）
                var cost = CalculateTokenCost(request.Model, inputTokens, outputTokens, cacheCreateTokens,
                    cacheReadTokens, httpContext);

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

                // 更新API Key使用统计
                var keyService = httpContext.RequestServices.GetRequiredService<ApiKeyService>();
                await keyService.UpdateApiKeyUsageAsync(apiKeyValue.Id, cost, cancellationToken);

                // 扣除钱包余额
                var walletService = httpContext.RequestServices.GetRequiredService<WalletService>();
                await walletService.DeductWalletAsync(apiKeyValue.UserId, cost, $"API调用费用 - {request.Model}", requestLogId);

                // 添加配额响应头
                AddQuotaHeaders(httpContext, apiKeyValue);
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
                    logger.LogWarning("OpenAI Responses账户 {AccountName} (ID: {AccountId}) 达到限流，限流解除时间：{RateLimitedUntil}",
                        account.Name, account.Id, rateLimitedUntil);
                }

                // 完成请求日志记录（限流失败）
                await requestLogService.CompleteRequestLogAsync(
                    requestLogId,
                    "rate_limited",
                    rateLimitEx.Message,
                    429,
                    cancellationToken: cancellationToken);

                // 返回429限流错误 - 增强版，包含替代账户信息
                httpContext.Response.StatusCode = 429;
                httpContext.Response.Headers["Retry-After"] = rateLimitEx.RateLimitInfo.RetryAfterSeconds.ToString();

                // 尝试获取限流详情和替代账户信息
                try
                {
                    var userAccountBindingService =
                        httpContext.RequestServices.GetRequiredService<UserAccountBindingService>();
                    var rateLimitInfo = await userAccountBindingService.GetRateLimitInfoAsync(
                        apiKeyValue.UserId, account?.Id ?? string.Empty, httpContext.RequestAborted);

                    await httpContext.Response.WriteAsJsonAsync(new
                    {
                        error = new
                        {
                            message =
                                $"OpenAI Responses账户 {account?.Name} 已达到限流，预计解除时间：{rateLimitInfo.EstimatedRecoveryTime:yyyy-MM-dd HH:mm:ss}",
                            type = "rate_limit_error",
                            code = "429",
                            details = new
                            {
                                account_name = account?.Name,
                                account_id = account?.Id,
                                rate_limited_until = rateLimitInfo.RateLimitedUntil.ToString("yyyy-MM-dd HH:mm:ss"),
                                retry_after_seconds = rateLimitInfo.RetryAfterSeconds,
                                alternative_accounts = rateLimitInfo.AlternativeAccounts,
                                service_type = "openai_responses"
                            }
                        }
                    }, cancellationToken);
                }
                catch (Exception ex)
                {
                    // 如果获取限流信息失败，返回基本错误信息
                    var logger = httpContext.RequestServices.GetRequiredService<ILogger<MessageService>>();
                    logger.LogWarning(ex, "获取OpenAI Responses限流信息失败，返回基本错误信息");

                    await httpContext.Response.WriteAsJsonAsync(new
                    {
                        error = new
                        {
                            message = $"OpenAI Responses账户 {account?.Name} 已达到限流，请稍后重试",
                            type = "rate_limit_error",
                            code = "429",
                            details = new
                            {
                                account_name = account?.Name,
                                account_id = account?.Id,
                                service_type = "openai_responses"
                            }
                        }
                    }, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                // 完成请求日志记录（失败）
                await requestLogService.CompleteRequestLogAsync(
                    requestLogId,
                    "error",
                    ex.Message,
                    500,
                    cancellationToken: cancellationToken);

                // 记录详细错误信息
                var logger = httpContext.RequestServices.GetRequiredService<ILogger<MessageService>>();
                logger.LogError(ex, "OpenAI Responses API调用失败: {ErrorMessage}", ex.Message);

                httpContext.Response.StatusCode = 500;
                await httpContext.Response.WriteAsJsonAsync(new
                {
                    // 返回claude需要的异常格式
                    error = new
                    {
                        message = ex.Message,
                        type = "server_error",
                        code = "500",
                        details = new
                        {
                            service_type = "openai_responses",
                            account_name = account?.Name,
                            account_id = account?.Id
                        }
                    }
                }, cancellationToken);
            }
        }

        /// <summary>
        ///     根据账户配置映射请求的模型
        /// </summary>
        /// <param name="requestedModel">请求的原始模型</param>
        /// <param name="account">使用的账户</param>
        /// <returns>映射后的模型名称，如果没有映射则返回原始模型</returns>
        private string MapRequestedModel(string requestedModel, Accounts? account)
        {
            // 如果账户为空或没有配置模型映射，返回原始模型
            if (account?.SupportedModels == null || account.SupportedModels.Count == 0) return requestedModel;

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
                            return targetModel;
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

        /// <summary>
        ///     预估请求费用（基于输入内容的粗略估算）
        /// </summary>
        private decimal EstimateRequestCost(ResponsesInput request, HttpContext httpContext)
        {
            try
            {
                // 粗略估算输入token数量（按字符数 / 4 估算，这是一个简化的方法）
                var estimatedInputTokens = 0;

                if (request.Inputs != null)
                    foreach (var message in request.Inputs)
                        if (message.Content is string textContent)
                        {
                            estimatedInputTokens += textContent.Length / 4; // 粗略估算
                        }
                        else if (message.Content is not string && message.Content is not null)
                        {
                            // 假设是对象数组，尝试转换为字符串计算
                            var contentString = message.Content;
                            if (!string.IsNullOrEmpty(contentString)) estimatedInputTokens += contentString.Length / 4;
                        }

                // 估算输出token数量（按最大输出的30%估算，避免过高预估）
                var maxTokens = request.MaxOutputTokens ?? 4096;
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
        ///     添加API Key配额响应头
        /// </summary>
        private void AddQuotaHeaders(HttpContext httpContext, ApiKey apiKey)
        {
        }

        /// <summary>
        ///     计算Token费用
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

    }

}
