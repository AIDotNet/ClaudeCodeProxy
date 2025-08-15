using ClaudeCodeProxy.Core;
using ClaudeCodeProxy.Domain;
using ClaudeCodeProxy.Host.Endpoints;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using ClaudeCodeProxy.Host.Models;

namespace ClaudeCodeProxy.Host.Services;

public class ApiKeyService(IContext context)
{
    public async Task<ApiKey?> GetApiKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var apiKey = await context.ApiKeys
            .Include(x => x.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.KeyValue == key, cancellationToken);

        if (apiKey != null)
        {
            await context.ApiKeys
                .Where(x => x.Id == apiKey.Id)
                .ExecuteUpdateAsync(x => x.SetProperty(y => y.LastUsedAt, DateTime.Now), cancellationToken);
        }

        return apiKey;
    }

    /// <summary>
    /// 获取所有API Keys
    /// </summary>
    public async Task<List<ApiKey>> GetAllApiKeysAsync(IUserContext userContext,
        CancellationToken cancellationToken = default)
    {
        return await context.ApiKeys
            .Where(x=>x.UserId == userContext.GetCurrentUserId())
            .Include(x => x.User)
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取指定用户的API Keys
    /// </summary>
    public async Task<List<ApiKey>> GetUserApiKeysAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.ApiKeys
            .Include(x => x.User)
            .Where(x => x.UserId == userId)
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据ID获取API Key
    /// </summary>
    public async Task<ApiKey?> GetApiKeyByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.ApiKeys
            .Include(x => x.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    /// <summary>
    /// 创建新的API Key
    /// </summary>
    public async Task<ApiKey> CreateApiKeyAsync(CreateApiKeyRequest request, Guid userId,
        CancellationToken cancellationToken = default)
    {
        var keyValue = GenerateApiKey();

        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId ?? userId, // 使用请求中的UserId或当前用户ID
            Name = request.Name,
            KeyValue = keyValue,
            Description = request.Description,
            Tags = request.Tags,
            TokenLimit = request.TokenLimit,
            RateLimitWindow = request.RateLimitWindow,
            RateLimitRequests = request.RateLimitRequests,
            ConcurrencyLimit = request.ConcurrencyLimit,
            DailyCostLimit = request.DailyCostLimit,
            MonthlyCostLimit = request.MonthlyCostLimit,
            TotalCostLimit = request.TotalCostLimit,
            ExpiresAt = request.ExpiresAt,
            Permissions = request.Permissions,
            ClaudeAccountId = request.ClaudeAccountId,
            ClaudeConsoleAccountId = request.ClaudeConsoleAccountId,
            GeminiAccountId = request.GeminiAccountId,
            EnableModelRestriction = request.EnableModelRestriction,
            RestrictedModels = request.RestrictedModels,
            EnableClientRestriction = request.EnableClientRestriction,
            AllowedClients = request.AllowedClients,
            IsEnabled = request.IsEnabled,
            Model = request.Model,
            Service = request.Service,
            CreatedAt = DateTime.Now
        };

        context.ApiKeys.Add(apiKey);
        await context.SaveAsync(cancellationToken);

        return apiKey;
    }

    /// <summary>
    /// 更新API Key
    /// </summary>
    public async Task<ApiKey?> UpdateApiKeyAsync(Guid id, UpdateApiKeyRequest request,
        CancellationToken cancellationToken = default)
    {
        var apiKey = await context.ApiKeys.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (apiKey == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(request.Name))
            apiKey.Name = request.Name;

        if (request.Description != null)
            apiKey.Description = request.Description;

        if (request.ExpiresAt.HasValue)
            apiKey.ExpiresAt = request.ExpiresAt.Value;

        if (request.IsEnabled.HasValue)
            apiKey.IsEnabled = request.IsEnabled.Value;

        if (request.DailyCostLimit.HasValue)
            apiKey.DailyCostLimit = request.DailyCostLimit.Value;

        if (request.MonthlyCostLimit.HasValue)
            apiKey.MonthlyCostLimit = request.MonthlyCostLimit.Value;

        if (request.TotalCostLimit.HasValue)
            apiKey.TotalCostLimit = request.TotalCostLimit.Value;

        if (request.Tags != null)
            apiKey.Tags = request.Tags;

        if (request.TokenLimit.HasValue)
            apiKey.TokenLimit = request.TokenLimit.Value;

        if (request.RateLimitWindow.HasValue)
            apiKey.RateLimitWindow = request.RateLimitWindow.Value;

        if (request.RateLimitRequests.HasValue)
            apiKey.RateLimitRequests = request.RateLimitRequests.Value;

        if (request.ConcurrencyLimit.HasValue)
            apiKey.ConcurrencyLimit = request.ConcurrencyLimit.Value;

        if (!string.IsNullOrEmpty(request.Permissions))
            apiKey.Permissions = request.Permissions;

        if (request.ClaudeAccountId != null)
            apiKey.ClaudeAccountId = request.ClaudeAccountId;

        if (request.ClaudeConsoleAccountId != null)
            apiKey.ClaudeConsoleAccountId = request.ClaudeConsoleAccountId;

        if (request.GeminiAccountId != null)
            apiKey.GeminiAccountId = request.GeminiAccountId;

        if (request.EnableModelRestriction.HasValue)
            apiKey.EnableModelRestriction = request.EnableModelRestriction.Value;

        if (request.RestrictedModels != null)
            apiKey.RestrictedModels = request.RestrictedModels;

        if (request.EnableClientRestriction.HasValue)
            apiKey.EnableClientRestriction = request.EnableClientRestriction.Value;

        if (request.AllowedClients != null)
            apiKey.AllowedClients = request.AllowedClients;

        if (!string.IsNullOrEmpty(request.Service))
            apiKey.Service = request.Service;

        apiKey.ModifiedAt = DateTime.Now;
        apiKey.Model = request.Model;

        await context.SaveAsync(cancellationToken);
        return apiKey;
    }

    /// <summary>
    /// 删除API Key
    /// </summary>
    public async Task<bool> DeleteApiKeyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var apiKey = await context.ApiKeys.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (apiKey == null)
        {
            return false;
        }

        context.ApiKeys.Remove(apiKey);
        await context.SaveAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// 启用API Key
    /// </summary>
    public async Task<bool> EnableApiKeyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await context.ApiKeys.Where(x => x.Id == id)
            .ExecuteUpdateAsync(x => x.SetProperty(a => a.IsEnabled, true)
                .SetProperty(a => a.ModifiedAt, DateTime.Now), cancellationToken);

        // 检查是否有记录被更新
        var apiKey = await context.ApiKeys.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return apiKey != null;
    }

    /// <summary>
    /// 禁用API Key
    /// </summary>
    public async Task<bool> DisableApiKeyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await context.ApiKeys.Where(x => x.Id == id)
            .ExecuteUpdateAsync(x => x.SetProperty(a => a.IsEnabled, false)
                .SetProperty(a => a.ModifiedAt, DateTime.Now), cancellationToken);

        // 检查是否有记录被更新
        var apiKey = await context.ApiKeys.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return apiKey != null;
    }

    /// <summary>
    /// 切换API Key启用状态
    /// </summary>
    public async Task<bool> ToggleApiKeyEnabledAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var apiKey = await context.ApiKeys.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (apiKey == null)
        {
            return false;
        }

        apiKey.IsEnabled = !apiKey.IsEnabled;
        apiKey.ModifiedAt = DateTime.Now;

        await context.SaveAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// 验证API Key
    /// </summary>
    public async Task<bool> ValidateApiKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var apiKey = await GetApiKeyAsync(key, cancellationToken);

        if (apiKey == null || !apiKey.IsEnabled)
            return false;

        if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTime.Now)
            return false;

        return true;
    }

    /// <summary>
    /// 获取API Key并刷新费用使用状态
    /// </summary>
    public async Task<ApiKey?> GetApiKeyWithRefreshedUsageAsync(string key, CancellationToken cancellationToken = default)
    {
        var apiKey = await context.ApiKeys
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.KeyValue == key, cancellationToken);

        if (apiKey == null)
            return null;

        // 刷新使用状态（重置过期的每日/月度使用量）
        await RefreshApiKeyUsageAsync(apiKey, cancellationToken);

        // 更新最后使用时间
        await context.ApiKeys
            .Where(x => x.Id == apiKey.Id)
            .ExecuteUpdateAsync(x => x.SetProperty(y => y.LastUsedAt, DateTime.Now), cancellationToken);

        return apiKey;
    }

    /// <summary>
    /// 刷新API Key的使用状态
    /// </summary>
    private async Task RefreshApiKeyUsageAsync(ApiKey apiKey, CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        var today = now.Date;
        var currentMonth = new DateTime(now.Year, now.Month, 1);
        
        bool needsUpdate = false;

        // 检查是否需要重置每日使用量
        var lastUsedDate = apiKey.LastUsedAt?.Date;
        if (lastUsedDate.HasValue && lastUsedDate.Value < today && apiKey.DailyCostUsed > 0)
        {
            apiKey.DailyCostUsed = 0;
            needsUpdate = true;
        }

        // 检查是否需要重置月度使用量
        var lastUsedMonth = apiKey.LastUsedAt.HasValue ? 
            new DateTime(apiKey.LastUsedAt.Value.Year, apiKey.LastUsedAt.Value.Month, 1) : 
            DateTime.MinValue;
        if (lastUsedMonth < currentMonth && apiKey.MonthlyCostUsed > 0)
        {
            apiKey.MonthlyCostUsed = 0;
            needsUpdate = true;
        }

        if (needsUpdate)
        {
            await context.SaveAsync(cancellationToken);
        }
    }

    /// <summary>
    /// 生成API Key
    /// </summary>
    private static string GenerateApiKey()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[64];
        rng.GetBytes(bytes);
        return "sk-ant-" + Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    /// <summary>
    /// 获取API Key账户绑定管理信息
    /// </summary>
    public async Task<ApiKeyBindingManagementDto?> GetApiKeyBindingManagementAsync(
        Guid apiKeyId,
        CancellationToken cancellationToken = default)
    {
        var apiKey = await context.ApiKeys
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == apiKeyId, cancellationToken);

        if (apiKey == null)
        {
            return null;
        }

        // 获取当前绑定的账户信息
        var accountBindings = new List<ApiKeyAccountBindingDto>();
        if (apiKey.AccountBindings != null && apiKey.AccountBindings.Any())
        {
            var boundAccountIds = apiKey.AccountBindings.Select(b => b.AccountId).ToList();
            var boundAccounts = await context.Accounts
                .Where(a => boundAccountIds.Contains(a.Id))
                .ToListAsync(cancellationToken);

            accountBindings = apiKey.AccountBindings.Select(binding =>
            {
                var account = boundAccounts.FirstOrDefault(a => a.Id == binding.AccountId);
                return new ApiKeyAccountBindingDto
                {
                    AccountId = binding.AccountId,
                    AccountName = account?.Name ?? "未知账户",
                    Platform = account?.Platform ?? "未知",
                    Priority = binding.Priority,
                    IsEnabled = binding.IsEnabled,
                    IsDefault = binding.AccountId == apiKey.DefaultAccountId
                };
            }).OrderBy(b => b.Priority).ToList();
        }

        // 获取可用于绑定的账户（用户拥有的和全局账户）
        var existingBoundAccountIds = apiKey.AccountBindings?.Select(b => b.AccountId).ToHashSet() ?? new HashSet<string>();
        var availableAccounts = await context.Accounts
            .Where(a => a.IsEnabled && 
                       (a.IsGlobal || a.OwnerUserId == apiKey.UserId) && 
                       !existingBoundAccountIds.Contains(a.Id))
            .ToListAsync(cancellationToken);

        var availableAccountDtos = availableAccounts.Select(account => new AvailableAccountDto
        {
            Id = account.Id,
            Name = account.Name,
            Platform = account.Platform,
            Description = account.Description,
            Status = account.Status,
            IsGlobal = account.IsGlobal,
            IsOwned = account.OwnerUserId == apiKey.UserId,
            SuggestedPriority = GetSuggestedPriority(account)
        }).ToList();

        return new ApiKeyBindingManagementDto
        {
            ApiKeyId = apiKeyId,
            ApiKeyName = apiKey.Name,
            UserId = apiKey.UserId,
            DefaultAccountId = apiKey.DefaultAccountId,
            AccountBindings = accountBindings,
            AvailableAccounts = availableAccountDtos
        };
    }

    /// <summary>
    /// 设置API Key默认账户
    /// </summary>
    public async Task<bool> SetDefaultAccountAsync(
        Guid apiKeyId,
        string accountId,
        CancellationToken cancellationToken = default)
    {
        var apiKey = await context.ApiKeys.FirstOrDefaultAsync(a => a.Id == apiKeyId, cancellationToken);
        if (apiKey == null)
        {
            return false;
        }

        // 验证账户存在且用户有权限使用
        var account = await context.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId && 
                                    a.IsEnabled && 
                                    (a.IsGlobal || a.OwnerUserId == apiKey.UserId), 
                               cancellationToken);

        if (account == null)
        {
            throw new ArgumentException($"账户不存在或用户无权限使用: {accountId}");
        }

        apiKey.DefaultAccountId = accountId;
        apiKey.ModifiedAt = DateTime.Now;

        await context.SaveAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// 更新API Key账户绑定
    /// </summary>
    public async Task<bool> UpdateAccountBindingsAsync(
        Guid apiKeyId,
        string? defaultAccountId,
        List<ApiKeyAccountBindingRequest> bindings,
        CancellationToken cancellationToken = default)
    {
        var apiKey = await context.ApiKeys.FirstOrDefaultAsync(a => a.Id == apiKeyId, cancellationToken);
        if (apiKey == null)
        {
            return false;
        }

        // 验证所有账户都存在且用户有权限使用
        var accountIds = bindings.Select(b => b.AccountId).ToList();
        if (!string.IsNullOrEmpty(defaultAccountId))
        {
            accountIds.Add(defaultAccountId);
        }

        var validAccounts = await context.Accounts
            .Where(a => accountIds.Contains(a.Id) && 
                       a.IsEnabled && 
                       (a.IsGlobal || a.OwnerUserId == apiKey.UserId))
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        var invalidAccountIds = accountIds.Except(validAccounts).ToList();
        if (invalidAccountIds.Any())
        {
            throw new ArgumentException($"以下账户不存在或用户无权限使用: {string.Join(", ", invalidAccountIds)}");
        }

        // 更新绑定信息
        var newBindings = bindings.Select(b => new ApiKeyAccountBinding
        {
            AccountId = b.AccountId,
            Priority = b.Priority,
            IsEnabled = b.IsEnabled
        }).ToList();

        apiKey.AccountBindings = newBindings;
        apiKey.DefaultAccountId = defaultAccountId;
        apiKey.ModifiedAt = DateTime.Now;

        await context.SaveAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// 获取建议的优先级
    /// </summary>
    private static int GetSuggestedPriority(Accounts account)
    {
        return account.AccountType.ToLower() switch
        {
            "dedicated" => 10, // 专属账户优先级高
            "shared" => 50,    // 共享账户中等优先级
            _ => 50
        };
    }
}