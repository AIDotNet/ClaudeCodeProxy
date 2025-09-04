using System.Text;
using System.Text.Json;
using ClaudeCodeProxy.Abstraction;
using ClaudeCodeProxy.Core;
using ClaudeCodeProxy.Domain;
using ClaudeCodeProxy.Host.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ClaudeCodeProxy.Host.Services;

/// <summary>
/// 账户服务实现
/// </summary>
public class AccountsService(IContext context, IMemoryCache memoryCache, ILogger<AccountsService> logger)
{
    /// <summary>
    /// 获取所有账户
    /// </summary>
    public async Task<List<Accounts>> GetAllAccountsAsync(CancellationToken cancellationToken = default)
    {
        return await context.Accounts
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据ID获取账户
    /// </summary>
    public async Task<Accounts?> GetAccountByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    /// <summary>
    /// 根据平台获取账户
    /// </summary>
    public async Task<List<Accounts>> GetAccountsByPlatformAsync(string platform,
        CancellationToken cancellationToken = default)
    {
        return await context.Accounts
            .AsNoTracking()
            .Where(x => x.Platform == platform)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 创建新账户
    /// </summary>
    public async Task<Accounts> CreateAccountAsync(
        string platform,
        CreateAccountRequest request, CancellationToken cancellationToken = default)
    {
        var account = new Accounts
        {
            Id = Guid.NewGuid().ToString(),
            Platform = platform,
            Name = request.Name,
            Description = request.Description,
            IsEnabled = true,
            CreatedAt = DateTime.Now,
            ApiKey = request.ApiKey,
            ApiUrl = request.ApiUrl,
            Proxy = request.Proxy,
            ClaudeAiOauth = request.ClaudeAiOauth,
            OpenAiOauth = request.OpenAiOauth,
            Priority = request.Priority,
            AccountType = request.AccountType,
        };

        await context.Accounts.AddAsync(account, cancellationToken);
        await context.SaveAsync(cancellationToken);

        return account;
    }

    /// <summary>
    /// 更新账户
    /// </summary>
    public async Task<Accounts?> UpdateAccountAsync(string id, UpdateAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var account = await context.Accounts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (account == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(request.Name))
            account.Name = request.Name;

        if (!string.IsNullOrEmpty(request.Description))
            account.Description = request.Description;

        if (request.IsActive.HasValue)
            account.IsEnabled = request.IsActive.Value;

        if (!string.IsNullOrEmpty(request.AccountType))
            account.AccountType = request.AccountType;

        if (request.Priority.HasValue)
            account.Priority = request.Priority.Value;

        if (!string.IsNullOrEmpty(request.ProjectId))
            account.ProjectId = request.ProjectId;

        if (!string.IsNullOrEmpty(request.ApiUrl))
            account.ApiUrl = request.ApiUrl;

        if (!string.IsNullOrEmpty(request.ApiKey))
            account.ApiKey = request.ApiKey;

        if (!string.IsNullOrEmpty(request.UserAgent))
            account.UserAgent = request.UserAgent;

        if (request.RateLimitDuration.HasValue)
            account.RateLimitDuration = request.RateLimitDuration.Value;

        if (request.SupportedModels != null)
        {
            // 将字典格式的模型映射转换为 List<string> 格式存储
            account.SupportedModels = request.SupportedModels
                .Where(kvp => !string.IsNullOrEmpty(kvp.Key) && !string.IsNullOrEmpty(kvp.Value))
                .Select(kvp => $"{kvp.Key}:{kvp.Value}")
                .ToList();

            // 确保转换后的数据格式正确 - 这应该生成 ["key:value", "key2:value2"] 的List<string>
            logger.LogDebug("SupportedModels转换结果: {SupportedModels}",
                string.Join(", ", account.SupportedModels.Select(x => $"\"{x}\"")));
        }

        account.ClaudeAiOauth = request.ClaudeAiOauth;
        account.OpenAiOauth = request.OpenAiOauth;

        if (request.GeminiOauth != null)
        {
            // 将对象序列化为JSON字符串存储
            if (request.GeminiOauth is string geminiOauthString)
            {
                account.GeminiOauth = geminiOauthString;
            }
            else
            {
                account.GeminiOauth =
                    System.Text.Json.JsonSerializer.Serialize(request.GeminiOauth, ThorJsonSerializer.DefaultOptions);
            }
        }

        account.Proxy = request.Proxy;

        account.ModifiedAt = DateTime.Now;

        await context.SaveAsync(cancellationToken);
        return account;
    }

    /// <summary>
    /// 删除账户
    /// </summary>
    public async Task<bool> DeleteAccountAsync(string id, CancellationToken cancellationToken = default)
    {
        var account = await context.Accounts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (account == null)
        {
            return false;
        }

        context.Accounts.Remove(account);
        await context.SaveAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// 更新账户状态
    /// </summary>
    public async Task<bool> UpdateAccountStatusAsync(string id, string status,
        CancellationToken cancellationToken = default)
    {
        var account = await context.Accounts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (account == null)
        {
            return false;
        }

        account.Status = status;
        account.ModifiedAt = DateTime.Now;

        await context.SaveAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// 启用账户
    /// </summary>
    public async Task<bool> EnableAccountAsync(string id, CancellationToken cancellationToken = default)
    {
        await context.Accounts.Where(x => x.Id == id)
            .ExecuteUpdateAsync(x => x.SetProperty(a => a.IsEnabled, true)
                .SetProperty(a => a.ModifiedAt, DateTime.Now), cancellationToken);

        // 检查是否有记录被更新
        var account = await context.Accounts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return account != null;
    }

    /// <summary>
    /// 禁用账户
    /// </summary>
    public async Task<bool> DisableAccountAsync(string id, CancellationToken cancellationToken = default)
    {
        await context.Accounts.Where(x => x.Id == id)
            .ExecuteUpdateAsync(x => x.SetProperty(a => a.IsEnabled, false)
                .SetProperty(a => a.ModifiedAt, DateTime.Now), cancellationToken);

        // 检查是否有记录被更新
        var account = await context.Accounts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return account != null;
    }

    /// <summary>
    /// 切换账户启用状态
    /// </summary>
    public async Task<bool> ToggleAccountEnabledAsync(string id, CancellationToken cancellationToken = default)
    {
        var account = await context.Accounts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (account == null)
        {
            return false;
        }

        account.IsEnabled = !account.IsEnabled;
        account.ModifiedAt = DateTime.Now;

        await context.SaveAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// 更新账户最后使用时间
    /// </summary>
    public async Task<bool> UpdateLastUsedAsync(string id, CancellationToken cancellationToken = default)
    {
        await context.Accounts.Where(x => x.Id == id)
            .ExecuteUpdateAsync(x => x.SetProperty(a => a.LastUsedAt, DateTime.Now)
                .SetProperty(a => a.UsageCount, a => a.UsageCount + 1)
                .SetProperty(a => a.ModifiedAt, DateTime.Now), cancellationToken);
        return true;
    }

    /// <summary>
    /// 设置账户限流状态
    /// </summary>
    public async Task<bool> SetRateLimitAsync(string id, DateTime rateLimitedUntil, string? error = null,
        CancellationToken cancellationToken = default)
    {
        await context.Accounts.Where(x => x.Id == id)
            .ExecuteUpdateAsync(x => x.SetProperty(a => a.Status, "rate_limited")
                .SetProperty(a => a.RateLimitedUntil, rateLimitedUntil)
                .SetProperty(a => a.LastError, error)
                .SetProperty(a => a.ModifiedAt, DateTime.Now), cancellationToken);

        return true;
    }

    /// <summary>
    /// 恢复限流账户状态
    /// </summary>
    public async Task<bool> RecoverRateLimitedAccountAsync(string id, CancellationToken cancellationToken = default)
    {
        var rowsAffected = await context.Accounts
            .Where(x => x.Id == id && x.Status == "rate_limited")
            .ExecuteUpdateAsync(x => x
                .SetProperty(a => a.Status, "active")
                .SetProperty(a => a.RateLimitedUntil, (DateTime?)null)
                .SetProperty(a => a.LastError, (string?)null)
                .SetProperty(a => a.ModifiedAt, DateTime.Now), cancellationToken);

        return rowsAffected > 0;
    }

    /// <summary>
    /// 批量恢复已过期的限流账户
    /// </summary>
    public async Task<int> RecoverExpiredRateLimitedAccountsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var rowsAffected = await context.Accounts
            .Where(x => x.Status == "rate_limited" &&
                        (x.RateLimitedUntil == null || x.RateLimitedUntil < now))
            .ExecuteUpdateAsync(x => x
                .SetProperty(a => a.Status, "active")
                .SetProperty(a => a.RateLimitedUntil, (DateTime?)null)
                .SetProperty(a => a.LastError, (string?)null)
                .SetProperty(a => a.ModifiedAt, DateTime.Now), cancellationToken);

        if (rowsAffected > 0)
        {
            logger.LogInformation("🔄 自动恢复了 {Count} 个已过期的限流账户", rowsAffected);
        }

        return rowsAffected;
    }

    /// <summary>
    /// 获取可用的账户（启用且未限流）
    /// </summary>
    public async Task<List<Accounts>> GetAvailableAccountsAsync(string? platform = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Accounts
            .AsNoTracking()
            .Where(x => x.IsEnabled &&
                        x.Status == "active" &&
                        (x.RateLimitedUntil == null || x.RateLimitedUntil < DateTime.Now));

        if (!string.IsNullOrEmpty(platform))
        {
            query = query.Where(x => x.Platform == platform);
        }

        return await query
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.LastUsedAt ?? DateTime.MinValue)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据算法获取一个可用账户
    /// </summary>
    /// <param name="apiKeyValue">API Key值</param>
    /// <param name="sessionHash">会话哈希</param>
    /// <param name="requestedModel">请求的模型</param>
    /// <param name="requestStream"></param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>选中的账户</returns>
    public async Task<Accounts?> SelectAccountForApiKey(ApiKey apiKeyValue, string sessionHash,
        string? requestedModel, bool requestStream, CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. 检查API Key的新绑定账户系统（最高优先级）
            var apiKeyBoundAccountIds = apiKeyValue.GetBoundAccountIds();
            if (apiKeyBoundAccountIds.Any())
            {
                foreach (var accountId in apiKeyBoundAccountIds)
                {
                    var boundAccount = await GetAccountByIdAsync(accountId, cancellationToken);
                    if (boundAccount != null && await IsAccountAvailableAsync(boundAccount, cancellationToken) &&
                        await DoesAccountSupportModelAsync(boundAccount, requestedModel, cancellationToken))
                    {
                        logger.LogInformation(
                            "🎯 使用API Key绑定的账户: {AccountName} ({AccountId}) for API key {ApiKeyName}",
                            boundAccount.Name, accountId, apiKeyValue.Name);

                        await UpdateLastUsedAsync(boundAccount.Id, cancellationToken);
                        return boundAccount;
                    }
                }

                logger.LogWarning("⚠️ API Key绑定的账户都不可用或不支持模型 {Model}，回退到用户绑定账户", requestedModel);
            }

            // 2. 检查用户绑定的账户（第二优先级）
            var userBoundAccounts =
                await GetUserBoundAccountsForApiKeyAsync(apiKeyValue, requestedModel, cancellationToken);
            if (userBoundAccounts.Any())
            {
                var selectedUserAccount = userBoundAccounts.FirstOrDefault(a => a.Status == "active" && a.IsEnabled);
                if (selectedUserAccount != null)
                {
                    logger.LogInformation(
                        "🎯 使用用户绑定的账户: {AccountName} ({AccountId}) for user {UserId}",
                        selectedUserAccount.Name, selectedUserAccount.Id, apiKeyValue.UserId);

                    await UpdateLastUsedAsync(selectedUserAccount.Id, cancellationToken);
                    return selectedUserAccount;
                }
            }

            // 3. 如果API Key绑定了专属Claude OAuth账户，尝试使用（向后兼容）
            if (!string.IsNullOrEmpty(apiKeyValue.ClaudeAccountId))
            {
                var boundAccount = await GetAccountByIdAsync(apiKeyValue.ClaudeAccountId, cancellationToken);
                if (boundAccount != null && await IsAccountAvailableAsync(boundAccount, cancellationToken))
                {
                    logger.LogInformation(
                        "🎯 使用绑定的专属Claude OAuth账户: {AccountName} ({AccountId}) for API key {ApiKeyName}",
                        boundAccount.Name, apiKeyValue.ClaudeAccountId, apiKeyValue.Name);

                    await UpdateLastUsedAsync(boundAccount.Id, cancellationToken);
                    return boundAccount;
                }
                else
                {
                    logger.LogWarning("⚠️ 绑定的Claude OAuth账户 {AccountId} 不可用，回退到账户池", apiKeyValue.ClaudeAccountId);
                }
            }

            // 4. 检查Claude Console账户绑定（向后兼容）
            if (!string.IsNullOrEmpty(apiKeyValue.ClaudeConsoleAccountId))
            {
                var boundConsoleAccount =
                    await GetAccountByIdAsync(apiKeyValue.ClaudeConsoleAccountId, cancellationToken);
                if (boundConsoleAccount != null &&
                    await IsAccountAvailableAsync(boundConsoleAccount, cancellationToken))
                {
                    logger.LogInformation(
                        "🎯 使用绑定的专属Claude Console账户: {AccountName} ({AccountId}) for API key {ApiKeyName}",
                        boundConsoleAccount.Name, apiKeyValue.ClaudeConsoleAccountId, apiKeyValue.Name);

                    await UpdateLastUsedAsync(boundConsoleAccount.Id, cancellationToken);
                    return boundConsoleAccount;
                }
                else
                {
                    logger.LogWarning("⚠️ 绑定的Claude Console账户 {AccountId} 不可用，回退到账户池",
                        apiKeyValue.ClaudeConsoleAccountId);
                }
            }

            // 5. 如果有会话哈希，检查是否有已映射的账户
            // if (!string.IsNullOrEmpty(sessionHash))
            // {
            //     var mappedAccount = await GetSessionMappingAsync(sessionHash, cancellationToken);
            //     if (mappedAccount != null)
            //     {
            //         // 验证映射的账户是否仍然可用
            //         if (await IsAccountAvailableAsync(mappedAccount, cancellationToken))
            //         {
            //             logger.LogInformation("🎯 使用粘性会话账户: {AccountName} ({AccountId}) for session {SessionHash}",
            //                 mappedAccount.Name, mappedAccount.Id, sessionHash);
            //
            //             await UpdateLastUsedAsync(mappedAccount.Id, cancellationToken);
            //             return mappedAccount;
            //         }
            //         else
            //         {
            //             logger.LogWarning("⚠️ 映射的账户 {AccountId} 不再可用，选择新账户", mappedAccount.Id);
            //             await DeleteSessionMappingAsync(sessionHash);
            //         }
            //     }
            // }

            // 4. 获取所有可用账户（传递请求的模型进行过滤）
            var availableAccounts =
                await GetAllAvailableAccountsAsync(apiKeyValue, requestedModel, requestStream, cancellationToken);

            if (availableAccounts.Count == 0)
            {
                // 提供更详细的错误信息
                var errorMessage = !string.IsNullOrEmpty(requestedModel)
                    ? $"没有可用的Claude账户支持请求的模型: {requestedModel}"
                    : "没有可用的Claude账户（官方或Console）";

                logger.LogError("❌ {ErrorMessage}", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            // 5. 按优先级和最后使用时间排序
            var sortedAccounts = SortAccountsByPriority(availableAccounts, requestedModel);

            // 6. 选择第一个账户
            var selectedAccount = sortedAccounts.First();

            // 7. 如果有会话哈希，建立新的映射
            if (!string.IsNullOrEmpty(sessionHash))
            {
                await SetSessionMappingAsync(sessionHash, selectedAccount, cancellationToken);
                logger.LogInformation("🎯 创建新的粘性会话映射: {AccountName} ({AccountId}) for session {SessionHash}",
                    selectedAccount.Name, selectedAccount.Id, sessionHash);
            }

            logger.LogInformation("🎯 选择账户: {AccountName} ({AccountId}) 优先级 {Priority} for API key {ApiKeyName}",
                selectedAccount.Name, selectedAccount.Id, selectedAccount.Priority, apiKeyValue.Name);

            await UpdateLastUsedAsync(selectedAccount.Id, cancellationToken);
            return selectedAccount;
        }
        catch (Exception error)
        {
            logger.LogError(error, "❌ 为API key选择账户失败");
            throw;
        }
    }

    /// <summary>
    /// 检查账户是否可用
    /// </summary>
    private async Task<bool> IsAccountAvailableAsync(Accounts account, CancellationToken cancellationToken = default)
    {
        // 刷新账户状态以获取最新信息
        var latestAccount = await GetAccountByIdAsync(account.Id, cancellationToken);
        if (latestAccount == null)
        {
            return false;
        }

        return latestAccount is { IsEnabled: true, Status: "active" } &&
               (latestAccount.RateLimitedUntil == null || latestAccount.RateLimitedUntil < DateTime.Now);
    }

    /// <summary>
    /// 获取所有可用账户（包含模型过滤）
    /// </summary>
    private async Task<List<Accounts>> GetAllAvailableAccountsAsync(ApiKey apiKey, string? requestedModel,
        bool requestStream,
        CancellationToken cancellationToken = default)
    {
        var query = context.Accounts
            .AsNoTracking()
            .Where(x => x.IsEnabled &&
                x.Status == "active" || x.Status == "rate_limited");

        query = query.Where(x =>
            x.Platform == "claude" || x.Platform == "claude-console" || x.Platform == "openai" ||
            x.Platform == "thor");

        var accounts = await query.ToListAsync(cancellationToken);

        accounts = accounts.Where(x =>
            (x.RateLimitedUntil == null || x.RateLimitedUntil < DateTime.Now)).ToList();

        if (!requestStream)
        {
            accounts = accounts.Where(x => x.OpenAiOauth == null).ToList();
        }

        // 如果指定了模型，进一步过滤支持该模型的账户
        if (!string.IsNullOrEmpty(requestedModel))
        {
            accounts = accounts.Where(account => DoesAccountSupportModel(account, requestedModel))
                .ToList();
            
            // 判断如果超过1个，优先选择有模型映射的账户
            if (accounts.Count > 1)
            {
                // 将有模型映射的账户排在前面
                var modelMappedAccounts = accounts.Where(a => a.SupportedModels != null && a.SupportedModels.Any(m =>
                    m.Split(':', 2)[0].Trim().Equals(requestedModel, StringComparison.OrdinalIgnoreCase))).ToList();
                
                var otherAccounts = accounts.Except(modelMappedAccounts).ToList();
                
                // 重新组合：有映射的在前，其他的在后
                accounts = modelMappedAccounts.Concat(otherAccounts).ToList();
            }
        }

        return accounts;
    }

    /// <summary>
    /// 检查账户是否支持指定模型
    /// </summary>
    private bool DoesAccountSupportModel(Accounts account, string model)
    {
        // 如果没有配置支持的模型，则认为支持所有模型
        if (account.SupportedModels == null || account.SupportedModels.Count == 0)
        {
            return true;
        }

        try
        {
            // 解析存储的模型映射格式：["from:to", "model1:target1"]
            foreach (var mapping in account.SupportedModels)
            {
                var parts = mapping.Split(':', 2);
                if (parts.Length == 2)
                {
                    var fromModel = parts[0].Trim();
                    // 检查请求的模型是否匹配映射中的源模型或目标模型
                    if (string.Equals(fromModel, model, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                // 也支持直接的模型名匹配（向后兼容）
                else if (string.Equals(mapping.Trim(), model, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        catch
        {
            // 解析失败时，默认支持
            return true;
        }
    }

    private List<Accounts> SortAccountsByPriority(List<Accounts> accounts, string? requestedModel)
    {
        var now = DateTime.Now;

        // 优先使用有OpenAI OAuth token的账户
        var oauthAccounts = accounts.Where(a => !string.IsNullOrEmpty(a.OpenAiOauth?.AccessToken)).ToList();
        if (oauthAccounts.Any())
        {
            accounts = oauthAccounts;
        }
        // 其次使用绑定了指定模型的账户
        else if (!string.IsNullOrEmpty(requestedModel))
        {
            var boundAccounts = accounts.Where(a => a.SupportedModels != null && a.SupportedModels.Any(m =>
                m.Split(':', 2)[0].Trim().Equals(requestedModel, StringComparison.OrdinalIgnoreCase))).ToList();
            if (boundAccounts.Any())
            {
                accounts = boundAccounts;
            }
        }
        return accounts
            .Select(account => new
            {
                Account = account,
                // 计算综合得分（分数越低越优先）
                Score = CalculateAccountScore(account, now)
            })
            .OrderBy(x => x.Score)
            .Select(x => x.Account)
            .ToList();
    }

    private double CalculateAccountScore(Accounts account, DateTime now)
    {
        double score = 0;

        // 1. 基础优先级权重 (0-100)
        score += account.Priority * 100;

        // 2. 使用频率权重 (0-50)
        score += Math.Min(account.UsageCount * 0.5, 50);

        // 3. 时间间隔权重 (0-100)
        var timeSinceLastUse = now - (account.LastUsedAt ?? DateTime.MinValue);
        if (timeSinceLastUse.TotalMinutes < 1) // 1分钟内使用过
            score += 100;
        else if (timeSinceLastUse.TotalMinutes < 5) // 5分钟内使用过
            score += 50;
        else if (timeSinceLastUse.TotalMinutes < 30) // 30分钟内使用过
            score += 20;

        // 4. 限流接近度权重 (0-200)
        if (account.RateLimitDuration > 0 && account.LastUsedAt.HasValue)
        {
            var timeSinceLastUseSeconds = timeSinceLastUse.TotalSeconds;
            var rateLimitApproachRatio = timeSinceLastUseSeconds / account.RateLimitDuration;
            if (rateLimitApproachRatio < 0.8) // 接近限流时间的80%
                score += 200 * (1 - rateLimitApproachRatio);
        }

        return score;
    }

    /// <summary>
    /// 获取会话映射的账户
    /// </summary>
    private async Task<Accounts?> GetSessionMappingAsync(string sessionHash,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"session_mapping_{sessionHash}";

        if (memoryCache.TryGetValue(cacheKey, out string? accountId) && !string.IsNullOrEmpty(accountId))
        {
            return await GetAccountByIdAsync(accountId, cancellationToken);
        }

        return null;
    }

    /// <summary>
    /// 设置会话映射
    /// </summary>
    private async Task SetSessionMappingAsync(string sessionHash, Accounts account,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"session_mapping_{sessionHash}";

        // 设置30分钟的会话映射缓存
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
            SlidingExpiration = TimeSpan.FromMinutes(15)
        };

        memoryCache.Set(cacheKey, account.Id, cacheOptions);

        await Task.CompletedTask; // 保持异步一致性
    }

    /// <summary>
    /// 删除会话映射
    /// </summary>
    private async Task DeleteSessionMappingAsync(string sessionHash)
    {
        var cacheKey = $"session_mapping_{sessionHash}";
        memoryCache.Remove(cacheKey);

        await Task.CompletedTask; // 保持异步一致性
    }

    /// <summary>
    /// 获取有效的访问令牌
    /// </summary>
    /// <param name="account">账户信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>有效的访问令牌</returns>
    public async Task<string> GetValidAccessTokenAsync(Accounts account, CancellationToken cancellationToken = default)
    {
        try
        {
            // 如果是Claude官方OAuth账户
            if (account is { Platform: "claude", ClaudeAiOauth: not null })
            {
                var oauth = account.ClaudeAiOauth;

                // 检查访问令牌是否即将过期（提前5分钟刷新）
                if (oauth.ExpiresAt > 0)
                {
                    var now = DateTimeOffset.Now.ToUnixTimeSeconds();
                    // 检查ExpiresAt是否在有效范围内（毫秒时间戳）
                    var isExpired = oauth.ExpiresAt == 0 ||
                                    (oauth.ExpiresAt > 0 && now >= (oauth.ExpiresAt - 120)); // 60秒提前刷新
                    if (isExpired)
                    {
                        logger.LogInformation("🔄 访问令牌即将过期，尝试刷新 for account {AccountId}", account.Id);

                        try
                        {
                            // 实现token刷新逻辑
                            var refreshedToken = await RefreshClaudeOAuthTokenAsync(account, cancellationToken);
                            if (!string.IsNullOrEmpty(refreshedToken))
                            {
                                logger.LogInformation("✅ 成功刷新访问令牌 for account {AccountId}", account.Id);
                                return refreshedToken;
                            }
                        }
                        catch (Exception refreshError)
                        {
                            logger.LogWarning("⚠️ Token刷新失败，使用现有token for account {AccountId}: {Error}",
                                account.Id, refreshError.Message);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(oauth.AccessToken))
                {
                    // 更新最后使用时间
                    await UpdateAccountLastUsedAsync(account.Id, cancellationToken);
                    return oauth.AccessToken;
                }
            }

            // 如果是Claude Console账户
            if (account.Platform == "claude-console" && !string.IsNullOrEmpty(account.ApiKey))
            {
                // 更新最后使用时间
                await UpdateAccountLastUsedAsync(account.Id, cancellationToken);
                return account.ApiKey;
            }

            // 如果是Gemini账户
            if (account.Platform == "gemini" && !string.IsNullOrEmpty(account.ApiKey))
            {
                // 更新最后使用时间
                await UpdateAccountLastUsedAsync(account.Id, cancellationToken);
                return account.ApiKey;
            }

            if (account.Platform == "openai" && !string.IsNullOrEmpty(account.ApiKey))
            {
                // 更新最后使用时间
                await UpdateAccountLastUsedAsync(account.Id, cancellationToken);
                return account.ApiKey;
            }

            // 如果是Thor账户
            if (account.Platform == "thor" && !string.IsNullOrEmpty(account.ApiKey))
            {
                // 更新最后使用时间
                await UpdateAccountLastUsedAsync(account.Id, cancellationToken);
                return account.ApiKey;
            }

            if (account.Platform == "openai" && !string.IsNullOrEmpty(account.OpenAiOauth?.AccessToken))
            {
                // 更新最后使用时间
                await UpdateAccountLastUsedAsync(account.Id, cancellationToken);
                return account.OpenAiOauth.AccessToken;
            }


            throw new InvalidOperationException($"无法为账户 {account.Id} ({account.Platform}) 获取有效的访问令牌");
        }
        catch (Exception ex)
        {
            logger.LogError("❌ 获取访问令牌失败 for account {AccountId}: {Error}", account.Id, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 刷新Claude OAuth访问令牌
    /// </summary>
    private async Task<string> RefreshClaudeOAuthTokenAsync(Accounts account,
        CancellationToken cancellationToken = default)
    {
        if (account.ClaudeAiOauth?.RefreshToken == null)
        {
            throw new InvalidOperationException("没有可用的刷新令牌");
        }

        try
        {
            using var httpClient = new HttpClient();

            // 设置请求头
            httpClient.DefaultRequestHeaders.Add("User-Agent", "claude-cli/1.0.65 (external, cli)");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
            httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            httpClient.DefaultRequestHeaders.Add("Referer", "https://claude.ai/");
            httpClient.DefaultRequestHeaders.Add("Origin", "https://claude.ai");

            var requestData = new
            {
                grant_type = "refresh_token",
                refresh_token = account.ClaudeAiOauth.RefreshToken,
                client_id = "9d1c250a-e61b-44d9-88ed-5944d1962f5e" // Claude OAuth客户端ID
            };

            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("https://console.anthropic.com/v1/oauth/token", content,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (tokenResponse.TryGetProperty("access_token", out var accessTokenElement) &&
                    tokenResponse.TryGetProperty("refresh_token", out var refreshTokenElement) &&
                    tokenResponse.TryGetProperty("expires_in", out var expiresInElement))
                {
                    var newAccessToken = accessTokenElement.GetString();
                    var newRefreshToken = refreshTokenElement.GetString();
                    var expiresIn = expiresInElement.GetInt64();

                    // 更新数据库中的OAuth信息
                    account.ClaudeAiOauth.AccessToken = newAccessToken;
                    account.ClaudeAiOauth.RefreshToken = newRefreshToken;
                    account.ClaudeAiOauth.ExpiresAt = DateTimeOffset.Now.AddSeconds(expiresIn).ToUnixTimeSeconds();

                    context.Accounts.Where(x => x.Id == account.Id)
                        .ExecuteUpdateAsync(x => x.SetProperty(a => a.ClaudeAiOauth, account.ClaudeAiOauth)
                                .SetProperty(x => x.ModifiedAt, DateTime.Now)
                                .SetProperty(x => x.LastUsedAt, DateTime.Now)
                                .SetProperty(x => x.ClaudeAiOauth, account.ClaudeAiOauth)
                                .SetProperty(x => x.UsageCount, x => x.UsageCount + 1),
                            cancellationToken);

                    logger.LogInformation("🔄 成功刷新Claude OAuth令牌 for account {AccountId}", account.Id);
                    return newAccessToken;
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError("❌ Claude OAuth令牌刷新失败，状态码: {StatusCode}, 响应: {Response}",
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"令牌刷新失败: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError("❌ 刷新Claude OAuth令牌时发生异常 for account {AccountId}: {Error}", account.Id, ex.Message);
            throw;
        }

        return null;
    }

    /// <summary>
    /// 更新账户最后使用时间
    /// </summary>
    private async Task UpdateAccountLastUsedAsync(string accountId, CancellationToken cancellationToken = default)
    {
        await context.Accounts.Where(x => x.Id == accountId)
            .ExecuteUpdateAsync(x => x.SetProperty(a => a.LastUsedAt, DateTime.Now)
                .SetProperty(a => a.UsageCount, a => a.UsageCount + 1)
                .SetProperty(a => a.ModifiedAt, DateTime.Now), cancellationToken);
    }

    /// <summary>
    /// 获取用户绑定的账户（用于API Key选择）
    /// </summary>
    private async Task<List<Accounts>> GetUserBoundAccountsForApiKeyAsync(ApiKey apiKeyValue,
        string? requestedModel,
        CancellationToken cancellationToken = default)
    {
        var query = from account in context.Accounts
            where account.IsEnabled &&
                  (account.IsGlobal ||
                   context.UserAccountBindings.Any(b =>
                       b.UserId == apiKeyValue.UserId &&
                       b.AccountId == account.Id &&
                       b.IsActive))
            select account;

        var accounts = await query
            .Include(a => a.UserBindings.Where(b => b.UserId == apiKeyValue.UserId))
            .OrderBy(a =>
                a.UserBindings.Any(b => b.UserId == apiKeyValue.UserId)
                    ? a.UserBindings.First(b => b.UserId == apiKeyValue.UserId).Priority
                    : int.MaxValue)
            .ThenBy(a => a.Priority)
            .ToListAsync(cancellationToken);

        // 过滤限流账户
        accounts = accounts.Where(x =>
            x.Status == "active" &&
            (x.RateLimitedUntil == null || x.RateLimitedUntil < DateTime.Now)).ToList();

        // 如果指定了模型，进一步过滤支持该模型的账户
        if (!string.IsNullOrEmpty(requestedModel))
        {
            accounts = accounts.Where(account => DoesAccountSupportModel(account, requestedModel)).ToList();
        }

        return accounts;
    }

    /// <summary>
    /// 异步检查账户是否支持指定模型
    /// </summary>
    private async Task<bool> DoesAccountSupportModelAsync(Accounts account,
        string? model,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(model))
        {
            return true;
        }

        return await Task.FromResult(DoesAccountSupportModel(account, model));
    }
}