using ClaudeCodeProxy.Core;
using ClaudeCodeProxy.Domain;
using ClaudeCodeProxy.Host.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ClaudeCodeProxy.Host.Services;

/// <summary>
///     用户账户绑定服务
/// </summary>
public class UserAccountBindingService(
    IContext context,
    IMemoryCache cache,
    ILogger<UserAccountBindingService> logger,
    AuditLogService auditLogService)
{
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(10);

    /// <summary>
    ///     绑定用户到账户
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="accountId">账户ID</param>
    /// <param name="priority">优先级</param>
    /// <param name="bindingType">绑定类型</param>
    /// <param name="remarks">备注</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>绑定关系</returns>
    public async Task<UserAccountBinding> BindUserToAccountAsync(
        Guid userId,
        string accountId,
        int priority = 50,
        string bindingType = "private",
        string? remarks = null,
        CancellationToken cancellationToken = default)
    {
        // 验证用户存在
        var userExists = await context.Users.AnyAsync(u => u.Id == userId, cancellationToken);
        if (!userExists) throw new ArgumentException($"用户不存在: {userId}");

        // 验证账户存在
        var account = await context.Accounts.FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);
        if (account == null) throw new ArgumentException($"账户不存在: {accountId}");

        // 检查是否已经绑定
        var existingBinding = await context.UserAccountBindings
            .FirstOrDefaultAsync(b => b.UserId == userId && b.AccountId == accountId, cancellationToken);

        if (existingBinding != null) throw new InvalidOperationException($"用户已经绑定到此账户: {accountId}");

        // 创建绑定关系
        var binding = new UserAccountBinding
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AccountId = accountId,
            BindingType = bindingType,
            Priority = priority,
            IsActive = true,
            Remarks = remarks,
            CreatedAt = DateTime.Now
        };

        context.UserAccountBindings.Add(binding);
        await context.SaveAsync(cancellationToken);

        // 记录审计日志
        await auditLogService.LogUserAccountBindingAsync(
            userId,
            "bind_account",
            accountId,
            newValues: new { accountId, priority, bindingType, remarks },
            cancellationToken: cancellationToken);

        // 清除相关缓存
        await InvalidateUserCacheAsync(userId);

        logger.LogInformation("用户 {UserId} 已绑定到账户 {AccountId}，优先级: {Priority}",
            userId, accountId, priority);

        return binding;
    }

    /// <summary>
    ///     解除用户和账户的绑定
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="accountId">账户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功解除绑定</returns>
    public async Task<bool> UnbindUserFromAccountAsync(
        Guid userId,
        string accountId,
        CancellationToken cancellationToken = default)
    {
        var binding = await context.UserAccountBindings
            .FirstOrDefaultAsync(b => b.UserId == userId && b.AccountId == accountId, cancellationToken);

        if (binding == null) return false;

        // 记录审计日志（在删除前记录旧值）
        await auditLogService.LogUserAccountBindingAsync(
            userId,
            "unbind_account",
            accountId,
            new
            {
                accountId = binding.AccountId,
                priority = binding.Priority,
                bindingType = binding.BindingType,
                remarks = binding.Remarks
            },
            cancellationToken: cancellationToken);

        context.UserAccountBindings.Remove(binding);
        await context.SaveAsync(cancellationToken);

        // 清除相关缓存
        await InvalidateUserCacheAsync(userId);

        logger.LogInformation("用户 {UserId} 已解除与账户 {AccountId} 的绑定", userId, accountId);
        return true;
    }

    /// <summary>
    ///     获取用户绑定的账户列表（按优先级排序）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="includeGlobal">是否包含全局账户</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>绑定的账户列表</returns>
    public async Task<List<Accounts>> GetUserBoundAccountsAsync(
        Guid userId,
        bool includeGlobal = true,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"user_accounts_{userId}_{includeGlobal}";

        if (cache.TryGetValue(cacheKey, out List<Accounts>? cachedAccounts) && cachedAccounts != null)
            return cachedAccounts;

        var query = context.Accounts.AsQueryable();

        // 构建查询：用户绑定的账户 + 全局账户
        var accountsQuery = from account in context.Accounts
            where account.IsEnabled &&
                  ((account.IsGlobal && includeGlobal) ||
                   context.UserAccountBindings.Any(b =>
                       b.UserId == userId &&
                       b.AccountId == account.Id &&
                       b.IsActive))
            select account;

        var accounts = await accountsQuery
            .Include(a => a.UserBindings.Where(b => b.UserId == userId))
            .OrderBy(a =>
                a.UserBindings.Any(b => b.UserId == userId)
                    ? a.UserBindings.First(b => b.UserId == userId).Priority
                    : int.MaxValue)
            .ThenBy(a => a.Priority)
            .ToListAsync(cancellationToken);

        // 缓存结果
        cache.Set(cacheKey, accounts, _cacheExpiration);

        return accounts;
    }

    /// <summary>
    ///     获取用户可见的账户列表（隐藏敏感信息）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="isAdmin">是否为管理员</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>可见的账户列表</returns>
    public async Task<List<Accounts>> GetVisibleAccountsForUserAsync(
        Guid userId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        List<Accounts> accounts;

        if (isAdmin)
        {
            // 管理员可以看到所有账户
            accounts = await context.Accounts
                .Include(a => a.UserBindings)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        else
        {
            // 普通用户只能看到自己拥有的、绑定的和全局账户
            accounts = await (from account in context.Accounts
                    where account.IsEnabled &&
                          (account.OwnerUserId == userId ||
                           account.IsGlobal ||
                           context.UserAccountBindings.Any(b =>
                               b.UserId == userId &&
                               b.AccountId == account.Id &&
                               b.IsActive))
                    select account)
                .Include(a => a.UserBindings.Where(b => b.UserId == userId))
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync(cancellationToken);

            // 对于非拥有的账户，隐藏敏感信息
            foreach (var account in accounts.Where(a => a.OwnerUserId != userId)) SanitizeAccountForUser(account);
        }

        return accounts;
    }

    /// <summary>
    ///     检查用户是否可以访问指定账户
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="accountId">账户ID</param>
    /// <param name="isAdmin">是否为管理员</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否可以访问</returns>
    public async Task<bool> CanUserAccessAccountAsync(
        Guid userId,
        string accountId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        if (isAdmin) return true;

        var account = await context.Accounts
            .Include(a => a.UserBindings.Where(b => b.UserId == userId))
            .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);

        if (account == null) return false;

        // 可以访问的条件：拥有者、全局账户或已绑定
        return account.OwnerUserId == userId ||
               account.IsGlobal ||
               account.UserBindings.Any(b => b.IsActive);
    }

    /// <summary>
    ///     更新用户账户绑定的优先级
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="accountId">账户ID</param>
    /// <param name="newPriority">新优先级</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功更新</returns>
    public async Task<bool> UpdateBindingPriorityAsync(
        Guid userId,
        string accountId,
        int newPriority,
        CancellationToken cancellationToken = default)
    {
        var binding = await context.UserAccountBindings
            .FirstOrDefaultAsync(b => b.UserId == userId && b.AccountId == accountId, cancellationToken);

        if (binding == null) return false;

        var oldPriority = binding.Priority;
        binding.Priority = newPriority;
        binding.ModifiedAt = DateTime.Now;

        await context.SaveAsync(cancellationToken);

        // 记录审计日志
        await auditLogService.LogUserAccountBindingAsync(
            userId,
            "update_priority",
            accountId,
            new { priority = oldPriority },
            new { priority = newPriority },
            cancellationToken: cancellationToken);

        // 清除相关缓存
        await InvalidateUserCacheAsync(userId);

        logger.LogInformation("用户 {UserId} 与账户 {AccountId} 的绑定优先级已更新为 {Priority}",
            userId, accountId, newPriority);

        return true;
    }

    /// <summary>
    ///     批量更新用户账户绑定
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="bindings">绑定信息列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新结果</returns>
    public async Task<bool> UpdateUserAccountBindingsAsync(
        Guid userId,
        List<UserAccountBindingRequest> bindings,
        CancellationToken cancellationToken = default)
    {
        // 获取现有绑定
        var existingBindings = await context.UserAccountBindings
            .Where(b => b.UserId == userId)
            .ToListAsync(cancellationToken);

        // 删除不再需要的绑定
        var bindingAccountIds = bindings.Select(b => b.AccountId).ToHashSet();
        var bindingsToRemove = existingBindings.Where(b => !bindingAccountIds.Contains(b.AccountId)).ToList();

        if (bindingsToRemove.Any()) context.UserAccountBindings.RemoveRange(bindingsToRemove);

        // 更新或创建绑定
        foreach (var bindingRequest in bindings)
        {
            var existingBinding = existingBindings.FirstOrDefault(b => b.AccountId == bindingRequest.AccountId);

            if (existingBinding != null)
            {
                // 更新现有绑定
                existingBinding.Priority = bindingRequest.Priority;
                existingBinding.IsActive = bindingRequest.IsEnabled;
                existingBinding.BindingType = bindingRequest.BindingType;
                existingBinding.ModifiedAt = DateTime.Now;
            }
            else
            {
                // 创建新绑定
                var newBinding = new UserAccountBinding
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    AccountId = bindingRequest.AccountId,
                    BindingType = bindingRequest.BindingType,
                    Priority = bindingRequest.Priority,
                    IsActive = bindingRequest.IsEnabled,
                    CreatedAt = DateTime.Now
                };

                context.UserAccountBindings.Add(newBinding);
            }
        }

        await context.SaveAsync(cancellationToken);

        // 记录审计日志
        await auditLogService.LogUserAccountBindingAsync(
            userId,
            "batch_update_bindings",
            "multiple",
            newValues: new
            {
                bindingCount = bindings.Count,
                accountIds = bindings.Select(b => b.AccountId).ToList()
            },
            cancellationToken: cancellationToken);

        // 清除相关缓存
        await InvalidateUserCacheAsync(userId);

        logger.LogInformation("用户 {UserId} 的账户绑定已批量更新", userId);
        return true;
    }

    /// <summary>
    ///     获取限流信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="accountId">账户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>限流信息</returns>
    public async Task<RateLimitInfoDto> GetRateLimitInfoAsync(
        Guid userId,
        string accountId,
        CancellationToken cancellationToken = default)
    {
        var account = await context.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);

        if (account?.RateLimitedUntil == null) throw new ArgumentException($"账户 {accountId} 未处于限流状态");

        // 获取用户可用的替代账户
        var alternativeAccounts = await GetUserBoundAccountsAsync(userId, true, cancellationToken);
        var availableAlternatives = alternativeAccounts
            .Where(a => a.Id != accountId && a.IsEnabled && a.Status == "active")
            .Select(a => a.Name)
            .ToList();

        var rateLimitedUntil = account.RateLimitedUntil.Value;
        var retryAfterSeconds = Math.Max(0, (int)(rateLimitedUntil - DateTime.Now).TotalSeconds);

        return new RateLimitInfoDto
        {
            RateLimitedUntil = rateLimitedUntil,
            EstimatedRecoveryTime = rateLimitedUntil,
            RetryAfterSeconds = retryAfterSeconds,
            AlternativeAccounts = availableAlternatives
        };
    }

    /// <summary>
    ///     获取用户账户绑定管理信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>账户绑定管理信息</returns>
    public async Task<AccountBindingManagementDto> GetAccountBindingManagementAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // 验证用户存在
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null) throw new ArgumentException($"用户不存在: {userId}");

        // 获取用户当前绑定的账户
        var currentBindings = await context.UserAccountBindings
            .Include(b => b.Account)
            .Where(b => b.UserId == userId)
            .OrderBy(b => b.Priority)
            .ToListAsync(cancellationToken);

        var boundAccounts = currentBindings.Select(binding => new BoundAccountDto
        {
            BindingId = binding.Id,
            AccountId = binding.AccountId,
            AccountName = binding.Account.Name,
            Platform = binding.Account.Platform,
            Status = binding.Account.Status,
            Priority = binding.Priority,
            BindingType = binding.BindingType,
            IsActive = binding.IsActive,
            Remarks = binding.Remarks,
            CreatedAt = binding.CreatedAt,
            CanUnbind = !binding.Account.IsGlobal
        }).ToList();

        // 获取可用于绑定的账户（排除已绑定的）
        var boundAccountIds = currentBindings.Select(b => b.AccountId).ToHashSet();
        var availableAccounts = await context.Accounts
            .Where(a => a.IsEnabled &&
                        (a.IsGlobal || a.OwnerUserId == userId) &&
                        !boundAccountIds.Contains(a.Id))
            .ToListAsync(cancellationToken);

        var availableAccountDtos = availableAccounts.Select(account => new AvailableAccountDto
        {
            Id = account.Id,
            Name = account.Name,
            Platform = account.Platform,
            Description = account.Description,
            Status = account.Status,
            IsGlobal = account.IsGlobal,
            IsOwned = account.OwnerUserId == userId,
            SuggestedPriority = GetSuggestedPriority(account)
        }).ToList();

        return new AccountBindingManagementDto
        {
            UserId = userId,
            Username = user.Username,
            BoundAccounts = boundAccounts,
            AvailableAccounts = availableAccountDtos
        };
    }

    /// <summary>
    ///     获取建议的优先级
    /// </summary>
    private static int GetSuggestedPriority(Accounts account)
    {
        return account.AccountType.ToLower() switch
        {
            "dedicated" => 10, // 专属账户优先级高
            "shared" => 50, // 共享账户中等优先级
            _ => 50
        };
    }

    /// <summary>
    ///     清除用户相关缓存
    /// </summary>
    /// <param name="userId">用户ID</param>
    private async Task InvalidateUserCacheAsync(Guid userId)
    {
        await Task.Run(() =>
        {
            try
            {
                cache.Remove($"user_accounts_{userId}_true");
                cache.Remove($"user_accounts_{userId}_false");

                // 清除相关的权限缓存
                cache.Remove($"user_permissions_{userId}");

                logger.LogDebug("已清除用户 {UserId} 的缓存", userId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "清除用户 {UserId} 缓存时发生异常", userId);
            }
        });
    }

    /// <summary>
    ///     清除账户相关缓存
    /// </summary>
    /// <param name="accountId">账户ID</param>
    private async Task InvalidateAccountCacheAsync(string accountId)
    {
        await Task.Run(() =>
        {
            try
            {
                // 清除所有可能包含此账户的用户缓存
                // 由于无法直接获取所有用户ID，这里使用一个全局缓存失效策略
                cache.Remove($"account_users_{accountId}");

                logger.LogDebug("已清除账户 {AccountId} 的相关缓存", accountId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "清除账户 {AccountId} 缓存时发生异常", accountId);
            }
        });
    }

    /// <summary>
    ///     为用户隐藏账户敏感信息
    /// </summary>
    /// <param name="account">账户对象</param>
    private static void SanitizeAccountForUser(Accounts account)
    {
        if (account.ApiKey != null && account.ApiKey.Length > 8)
            account.ApiKey = account.ApiKey[..4] + "****" + account.ApiKey[^4..];

        // 隐藏OAuth信息
        account.ClaudeAiOauth = null;
        account.GeminiOauth = null;

        // 隐藏代理配置
        account.Proxy = null;
    }
}