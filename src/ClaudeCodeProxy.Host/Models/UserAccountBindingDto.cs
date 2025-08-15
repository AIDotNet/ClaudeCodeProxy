namespace ClaudeCodeProxy.Host.Models;

/// <summary>
/// 用户账户绑定数据传输对象
/// </summary>
public class UserAccountBindingDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string AccountId { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string BindingType { get; set; } = string.Empty;
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

/// <summary>
/// 绑定账户请求
/// </summary>
public class BindAccountRequest
{
    public string AccountId { get; set; } = string.Empty;
    public int Priority { get; set; } = 50;
    public string BindingType { get; set; } = "private";
    public string? Remarks { get; set; }
}

/// <summary>
/// 用户账户绑定请求
/// </summary>
public class UserAccountBindingRequest
{
    public string AccountId { get; set; } = string.Empty;
    public string BindingType { get; set; } = "private";
    public int Priority { get; set; } = 50;
    public bool IsEnabled { get; set; } = true;
    public string? Remarks { get; set; }
}

/// <summary>
/// 批量更新用户账户绑定请求
/// </summary>
public class UpdateUserAccountBindingsRequest
{
    public List<UserAccountBindingRequest> AccountBindings { get; set; } = new();
}

/// <summary>
/// 限流信息数据传输对象
/// </summary>
public class RateLimitInfoDto
{
    public DateTime RateLimitedUntil { get; set; }
    public DateTime EstimatedRecoveryTime { get; set; }
    public int RetryAfterSeconds { get; set; }
    public List<string> AlternativeAccounts { get; set; } = new();
}