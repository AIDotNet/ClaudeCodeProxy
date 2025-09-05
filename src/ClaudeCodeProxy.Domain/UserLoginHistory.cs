using System.ComponentModel.DataAnnotations;

namespace ClaudeCodeProxy.Domain;

public class UserLoginHistory : Entity<int>
{
    public Guid UserId { get; set; }

    [MaxLength(50)] public string? LoginType { get; set; } // password, github, gitee, google

    [MaxLength(45)] public string? IpAddress { get; set; }

    [MaxLength(500)] public string? UserAgent { get; set; }

    [MaxLength(50)] public string? City { get; set; }

    [MaxLength(50)] public string? Country { get; set; }

    public bool Success { get; set; } = true;

    [MaxLength(200)] public string? FailureReason { get; set; }

    // 导航属性
    public virtual User User { get; set; } = null!;
}