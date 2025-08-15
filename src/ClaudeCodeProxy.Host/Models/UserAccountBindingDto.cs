using System.ComponentModel.DataAnnotations;

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
    [Required(ErrorMessage = "账户ID不能为空")]
    [MaxLength(100, ErrorMessage = "账户ID长度不能超过100个字符")]
    public string AccountId { get; set; } = string.Empty;
    
    [Range(1, 100, ErrorMessage = "优先级必须在1-100之间")]
    public int Priority { get; set; } = 50;
    
    [Required(ErrorMessage = "绑定类型不能为空")]
    [RegularExpression("^(private|shared)$", ErrorMessage = "绑定类型只能是 'private' 或 'shared'")]
    public string BindingType { get; set; } = "private";
    
    [MaxLength(500, ErrorMessage = "备注长度不能超过500个字符")]
    public string? Remarks { get; set; }
}

/// <summary>
/// 用户账户绑定请求
/// </summary>
public class UserAccountBindingRequest
{
    [Required(ErrorMessage = "账户ID不能为空")]
    [MaxLength(100, ErrorMessage = "账户ID长度不能超过100个字符")]
    public string AccountId { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "绑定类型不能为空")]
    [RegularExpression("^(private|shared)$", ErrorMessage = "绑定类型只能是 'private' 或 'shared'")]
    public string BindingType { get; set; } = "private";
    
    [Range(1, 100, ErrorMessage = "优先级必须在1-100之间")]
    public int Priority { get; set; } = 50;
    
    public bool IsEnabled { get; set; } = true;
    
    [MaxLength(500, ErrorMessage = "备注长度不能超过500个字符")]
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

/// <summary>
/// 更新绑定优先级请求
/// </summary>
public class UpdateBindingPriorityRequest
{
    [Range(1, 100, ErrorMessage = "优先级必须在1-100之间")]
    public int Priority { get; set; }
}

/// <summary>
/// 设置默认账户请求
/// </summary>
public class SetDefaultAccountRequest
{
    [Required(ErrorMessage = "账户ID不能为空")]
    [MaxLength(100, ErrorMessage = "账户ID长度不能超过100个字符")]
    public string AccountId { get; set; } = string.Empty;
}

/// <summary>
/// API Key账户绑定请求
/// </summary>
public class ApiKeyAccountBindingRequest
{
    [Required(ErrorMessage = "账户ID不能为空")]
    [MaxLength(100, ErrorMessage = "账户ID长度不能超过100个字符")]
    public string AccountId { get; set; } = string.Empty;
    
    [Range(1, 100, ErrorMessage = "优先级必须在1-100之间")]
    public int Priority { get; set; } = 50;
    
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// 更新API Key账户绑定请求
/// </summary>
public class UpdateApiKeyAccountBindingsRequest
{
    [MaxLength(100, ErrorMessage = "默认账户ID长度不能超过100个字符")]
    public string? DefaultAccountId { get; set; }
    
    public List<ApiKeyAccountBindingRequest> AccountBindings { get; set; } = new();
}