using ClaudeCodeProxy.Domain;
using ClaudeCodeProxy.Host.Models;
using Mapster;

namespace ClaudeCodeProxy.Host.Mappings;

/// <summary>
///     账户映射配置
/// </summary>
public class AccountMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Accounts -> AccountDisplayDto 映射
        config.NewConfig<Accounts, AccountDisplayDto>()
            .Map(dest => dest.ApiKeyDisplay, src => GetMaskedApiKey(src.ApiKey))
            .Map(dest => dest.RateLimitInfo, src => MapRateLimitInfo(src))
            .Map(dest => dest.IsOwned, src => false) // 默认值，需要在具体使用时设置
            .Map(dest => dest.UserBinding, src => (UserAccountBindingInfo)null); // 默认值，需要在具体使用时设置

        // UserAccountBinding -> UserAccountBindingInfo 映射
        config.NewConfig<UserAccountBinding, UserAccountBindingInfo>()
            .Map(dest => dest.BindingId, src => src.Id);

        // UserAccountBinding -> BoundAccountDto 映射，需要连接Account信息
        config.NewConfig<(UserAccountBinding binding, Accounts account), BoundAccountDto>()
            .Map(dest => dest.BindingId, src => src.binding.Id)
            .Map(dest => dest.AccountId, src => src.binding.AccountId)
            .Map(dest => dest.AccountName, src => src.account.Name)
            .Map(dest => dest.Platform, src => src.account.Platform)
            .Map(dest => dest.Status, src => src.account.Status)
            .Map(dest => dest.Priority, src => src.binding.Priority)
            .Map(dest => dest.BindingType, src => src.binding.BindingType)
            .Map(dest => dest.IsActive, src => src.binding.IsActive)
            .Map(dest => dest.Remarks, src => src.binding.Remarks)
            .Map(dest => dest.CreatedAt, src => src.binding.CreatedAt)
            .Map(dest => dest.CanUnbind, src => !src.account.IsGlobal);

        // Accounts -> AvailableAccountDto 映射
        config.NewConfig<Accounts, AvailableAccountDto>()
            .Map(dest => dest.SuggestedPriority, src => GetSuggestedPriority(src))
            .Map(dest => dest.IsOwned, src => false); // 默认值，需要在具体使用时设置

        // ApiKeyAccountBinding -> ApiKeyAccountBindingDto 映射
        config.NewConfig<(ApiKeyAccountBinding binding, Accounts account, bool isDefault), ApiKeyAccountBindingDto>()
            .Map(dest => dest.AccountId, src => src.binding.AccountId)
            .Map(dest => dest.AccountName, src => src.account.Name)
            .Map(dest => dest.Platform, src => src.account.Platform)
            .Map(dest => dest.Priority, src => src.binding.Priority)
            .Map(dest => dest.IsEnabled, src => src.binding.IsEnabled)
            .Map(dest => dest.IsDefault, src => src.isDefault);

        // UserAccountBinding -> UserAccountBindingDto 映射
        config.NewConfig<(UserAccountBinding binding, Accounts account), UserAccountBindingDto>()
            .Map(dest => dest.Id, src => src.binding.Id)
            .Map(dest => dest.UserId, src => src.binding.UserId)
            .Map(dest => dest.AccountId, src => src.binding.AccountId)
            .Map(dest => dest.AccountName, src => src.account.Name)
            .Map(dest => dest.Platform, src => src.account.Platform)
            .Map(dest => dest.BindingType, src => src.binding.BindingType)
            .Map(dest => dest.Priority, src => src.binding.Priority)
            .Map(dest => dest.IsActive, src => src.binding.IsActive)
            .Map(dest => dest.Remarks, src => src.binding.Remarks)
            .Map(dest => dest.CreatedAt, src => src.binding.CreatedAt)
            .Map(dest => dest.ModifiedAt, src => src.binding.ModifiedAt);
    }

    /// <summary>
    ///     获取脱敏的API Key显示
    /// </summary>
    private static string? GetMaskedApiKey(string? apiKey)
    {
        if (string.IsNullOrEmpty(apiKey) || apiKey.Length <= 8) return "****";

        return $"{apiKey[..4]}****{apiKey[^4..]}";
    }

    /// <summary>
    ///     映射限流信息
    /// </summary>
    private static RateLimitDisplayInfo? MapRateLimitInfo(Accounts account)
    {
        var isRateLimited = account.Status == "rate_limited" &&
                            account.RateLimitedUntil.HasValue &&
                            account.RateLimitedUntil > DateTime.Now;

        if (!isRateLimited && string.IsNullOrEmpty(account.LastError)) return null;

        return new RateLimitDisplayInfo
        {
            IsRateLimited = isRateLimited,
            RateLimitedUntil = account.RateLimitedUntil,
            RetryAfterMinutes = isRateLimited && account.RateLimitedUntil.HasValue
                ? Math.Max(0, (int)(account.RateLimitedUntil.Value - DateTime.Now).TotalMinutes)
                : null,
            LastError = account.LastError
        };
    }

    /// <summary>
    ///     获取建议的优先级
    /// </summary>
    private static int GetSuggestedPriority(Accounts account)
    {
        // 根据账户类型和平台建议优先级
        return account.AccountType.ToLower() switch
        {
            "dedicated" => 10, // 专属账户优先级高
            "shared" => 50, // 共享账户中等优先级
            _ => 50
        };
    }
}