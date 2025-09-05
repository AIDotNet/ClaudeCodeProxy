namespace ClaudeCodeProxy.Host.Models;

/// <summary>
///     用户个人资料DTO
/// </summary>
public class UserProfileDto
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
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public WalletProfileDto Wallet { get; set; } = new();
}

/// <summary>
///     钱包个人资料DTO
/// </summary>
public class WalletProfileDto
{
    public decimal Balance { get; set; }
    public decimal TotalUsed { get; set; }
    public decimal TotalRecharged { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? LastUsedAt { get; set; }
    public DateTime? LastRechargedAt { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
///     个人仪表板DTO
/// </summary>
public class ProfileDashboardDto
{
    public Guid UserId { get; set; }
    public WalletStatisticsDto Wallet { get; set; } = new();
    public UserRequestStatisticsDto Requests { get; set; } = new();
    public int ApiKeyCount { get; set; }
    public int ActiveApiKeyCount { get; set; }
    public DateTime LastUpdateTime { get; set; }
}

/// <summary>
///     更新个人资料请求
/// </summary>
public class UpdateProfileRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Avatar { get; set; }
}