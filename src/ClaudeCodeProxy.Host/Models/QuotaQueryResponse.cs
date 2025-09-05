namespace ClaudeCodeProxy.Host.Models;

/// <summary>
///     额度查询响应模型
/// </summary>
public class QuotaQueryResponse
{
    /// <summary>
    ///     每日额度限制
    /// </summary>
    public decimal? DailyCostLimit { get; set; }

    /// <summary>
    ///     每日已使用额度
    /// </summary>
    public decimal? DailyCostUsed { get; set; }

    /// <summary>
    ///     每日剩余额度
    /// </summary>
    public decimal? DailyAvailable { get; set; }

    /// <summary>
    ///     月度额度限制
    /// </summary>
    public decimal? MonthlyCostLimit { get; set; }

    /// <summary>
    ///     月度已使用额度
    /// </summary>
    public decimal? MonthlyCostUsed { get; set; }

    /// <summary>
    ///     月度剩余额度
    /// </summary>
    public decimal? MonthlyAvailable { get; set; }

    /// <summary>
    ///     总额度限制
    /// </summary>
    public decimal? TotalCostLimit { get; set; }

    /// <summary>
    ///     总已使用额度
    /// </summary>
    public decimal? TotalCostUsed { get; set; }

    /// <summary>
    ///     总剩余额度
    /// </summary>
    public decimal? TotalAvailable { get; set; }

    /// <summary>
    ///     组织信息
    /// </summary>
    public OrganizationInfo? Organization { get; set; }

    /// <summary>
    ///     账户绑定信息
    /// </summary>
    public AccountBindingInfo? AccountBinding { get; set; }
}

/// <summary>
///     组织信息
/// </summary>
public class OrganizationInfo
{
    /// <summary>
    ///     组织名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     组织ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
}

/// <summary>
///     账户绑定信息
/// </summary>
public class AccountBindingInfo
{
    /// <summary>
    ///     是否已绑定
    /// </summary>
    public bool IsBound { get; set; }

    /// <summary>
    ///     绑定的账户名称
    /// </summary>
    public string? AccountName { get; set; }

    /// <summary>
    ///     绑定的账户ID
    /// </summary>
    public string? AccountId { get; set; }

    /// <summary>
    ///     账户状态
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    ///     绑定时间
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    ///     过期时间
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    ///     速率限制信息
    /// </summary>
    public RateLimitingInfo? RateLimiting { get; set; }

    /// <summary>
    ///     限流解除时间
    /// </summary>
    public DateTime? RateLimitedUntil { get; set; }
}

/// <summary>
///     速率限制信息
/// </summary>
public class RateLimitingInfo
{
    /// <summary>
    ///     是否启用限流
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    ///     每分钟请求数限制
    /// </summary>
    public int? RequestsPerMinute { get; set; }

    /// <summary>
    ///     每小时请求数限制
    /// </summary>
    public int? RequestsPerHour { get; set; }

    /// <summary>
    ///     每天请求数限制
    /// </summary>
    public int? RequestsPerDay { get; set; }

    /// <summary>
    ///     当前使用量
    /// </summary>
    public CurrentUsage? CurrentUsage { get; set; }

    /// <summary>
    ///     重置时间
    /// </summary>
    public ResetTimes? ResetTimes { get; set; }
}

/// <summary>
///     当前使用量
/// </summary>
public class CurrentUsage
{
    /// <summary>
    ///     分钟内使用量
    /// </summary>
    public int Minute { get; set; }

    /// <summary>
    ///     小时内使用量
    /// </summary>
    public int Hour { get; set; }

    /// <summary>
    ///     天内使用量
    /// </summary>
    public int Day { get; set; }
}

/// <summary>
///     重置时间
/// </summary>
public class ResetTimes
{
    /// <summary>
    ///     分钟重置时间
    /// </summary>
    public string? Minute { get; set; }

    /// <summary>
    ///     小时重置时间
    /// </summary>
    public string? Hour { get; set; }

    /// <summary>
    ///     天重置时间
    /// </summary>
    public string? Day { get; set; }
}