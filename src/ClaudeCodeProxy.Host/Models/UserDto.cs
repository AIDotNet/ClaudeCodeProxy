namespace ClaudeCodeProxy.Host.Models;

/// <summary>
///     用户数据传输对象
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Avatar { get; set; }
    public bool IsActive { get; set; }
    public bool EmailConfirmed { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? Provider { get; set; }
    public string? ProviderId { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;

    public RoleDto? Role { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

/// <summary>
///     创建用户请求
/// </summary>
public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public int RoleId { get; set; } = 2; // 默认为用户角色
}

/// <summary>
///     用户注册请求
/// </summary>
public class RegisterUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? InvitationCode { get; set; }
}

/// <summary>
///     更新用户请求
/// </summary>
public class UpdateUserRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Avatar { get; set; }
    public bool IsActive { get; set; }
    public bool EmailConfirmed { get; set; }
    public int RoleId { get; set; }

    /// <summary>
    ///     账户绑定信息（可选）
    /// </summary>
    public List<UserAccountBindingRequest>? AccountBindings { get; set; }
}

/// <summary>
///     修改密码请求
/// </summary>
public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
///     重置密码请求
/// </summary>
public class ResetPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
///     用户登录历史DTO
/// </summary>
public class UserLoginHistoryDto
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string? LoginType { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
}