using System.ComponentModel.DataAnnotations;

namespace ClaudeCodeProxy.Domain;

public class User : Entity<Guid>
{
    [Required] [MaxLength(50)] public string Username { get; set; } = string.Empty;

    [Required] [MaxLength(100)] public string Email { get; set; } = string.Empty;

    [MaxLength(255)] public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(50)] public string? FirstName { get; set; }

    [MaxLength(50)] public string? LastName { get; set; }

    [MaxLength(200)] public string? Avatar { get; set; }

    public bool IsActive { get; set; } = true;

    public bool EmailConfirmed { get; set; } = false;

    public DateTime? LastLoginAt { get; set; }

    // 第三方登录相关
    [MaxLength(50)] public string? Provider { get; set; }

    [MaxLength(100)] public string? ProviderId { get; set; }

    // 邀请系统相关
    [MaxLength(8)] public string InvitationCode { get; set; } = string.Empty;

    public Guid? InvitedByUserId { get; set; }

    // 外键
    public int RoleId { get; set; }

    // 导航属性
    public virtual Role Role { get; set; } = null!;
    public virtual User? InvitedByUser { get; set; }
    public virtual ICollection<User> InvitedUsers { get; set; } = new List<User>();
    public virtual ICollection<UserLoginHistory> LoginHistories { get; set; } = new List<UserLoginHistory>();
    public virtual Wallet? Wallet { get; set; }
    public virtual ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();
    public virtual ICollection<RequestLog> RequestLogs { get; set; } = new List<RequestLog>();
    public virtual ICollection<StatisticsSnapshot> StatisticsSnapshots { get; set; } = new List<StatisticsSnapshot>();
    public virtual ICollection<UserAccountBinding> AccountBindings { get; set; } = new List<UserAccountBinding>();
    public virtual ICollection<Accounts> OwnedAccounts { get; set; } = new List<Accounts>();
}