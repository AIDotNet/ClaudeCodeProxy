using ClaudeCodeProxy.Domain;

namespace ClaudeCodeProxy.Host.Models;

/// <summary>
/// 账户显示DTO（脱敏版本，用于前端列表显示）
/// </summary>
public class AccountDisplayDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string AccountType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Priority { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsGlobal { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public long UsageCount { get; set; }
    
    /// <summary>
    /// 脱敏的API Key显示（只显示前后4位）
    /// </summary>
    public string? ApiKeyDisplay { get; set; }
    
    /// <summary>
    /// 是否为当前用户拥有的账户
    /// </summary>
    public bool IsOwned { get; set; }
    
    /// <summary>
    /// 用户绑定信息（如果存在）
    /// </summary>
    public UserAccountBindingInfo? UserBinding { get; set; }
    
    /// <summary>
    /// 支持的模型列表（用于前端显示）
    /// </summary>
    public List<string>? SupportedModels { get; set; }
    
    /// <summary>
    /// 限流信息
    /// </summary>
    public RateLimitDisplayInfo? RateLimitInfo { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

/// <summary>
/// 用户账户绑定信息（用于前端显示）
/// </summary>
public class UserAccountBindingInfo
{
    public Guid BindingId { get; set; }
    public int Priority { get; set; }
    public string BindingType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 限流信息显示
/// </summary>
public class RateLimitDisplayInfo
{
    public bool IsRateLimited { get; set; }
    public DateTime? RateLimitedUntil { get; set; }
    public int? RetryAfterMinutes { get; set; }
    public string? LastError { get; set; }
}

/// <summary>
/// 账户绑定管理DTO（用于前端账户绑定管理界面）
/// </summary>
public class AccountBindingManagementDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户当前绑定的账户列表
    /// </summary>
    public List<BoundAccountDto> BoundAccounts { get; set; } = new();
    
    /// <summary>
    /// 可用于绑定的账户列表
    /// </summary>
    public List<AvailableAccountDto> AvailableAccounts { get; set; } = new();
}

/// <summary>
/// 已绑定账户DTO
/// </summary>
public class BoundAccountDto
{
    public Guid BindingId { get; set; }
    public string AccountId { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string BindingType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// 是否可以解除绑定（如果是全局账户则不能解除）
    /// </summary>
    public bool CanUnbind { get; set; } = true;
}

/// <summary>
/// 可绑定账户DTO
/// </summary>
public class AvailableAccountDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsGlobal { get; set; }
    public bool IsOwned { get; set; }
    
    /// <summary>
    /// 建议的优先级（基于账户类型和重要性）
    /// </summary>
    public int SuggestedPriority { get; set; } = 50;
}

/// <summary>
/// API Key绑定管理DTO
/// </summary>
public class ApiKeyBindingManagementDto
{
    public Guid ApiKeyId { get; set; }
    public string ApiKeyName { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    
    /// <summary>
    /// 默认绑定账户ID
    /// </summary>
    public string? DefaultAccountId { get; set; }
    
    /// <summary>
    /// 账户绑定列表（按优先级排序）
    /// </summary>
    public List<ApiKeyAccountBindingDto> AccountBindings { get; set; } = new();
    
    /// <summary>
    /// 可用于绑定的账户列表
    /// </summary>
    public List<AvailableAccountDto> AvailableAccounts { get; set; } = new();
}

/// <summary>
/// API Key账户绑定DTO
/// </summary>
public class ApiKeyAccountBindingDto
{
    public string AccountId { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public int Priority { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsDefault { get; set; }
}