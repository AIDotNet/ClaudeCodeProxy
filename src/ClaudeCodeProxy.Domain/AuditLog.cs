using System.ComponentModel.DataAnnotations;

namespace ClaudeCodeProxy.Domain;

/// <summary>
///     审计日志实体类
/// </summary>
public class AuditLog : Entity<Guid>
{
    /// <summary>
    ///     用户ID（执行操作的用户）
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    ///     操作类型
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    ///     资源类型
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    ///     资源ID
    /// </summary>
    [MaxLength(100)]
    public string? ResourceId { get; set; }

    /// <summary>
    ///     操作详情（JSON格式）
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    ///     操作前的数据（JSON格式）
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    ///     操作后的数据（JSON格式）
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    ///     IP地址
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    ///     用户代理
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    ///     操作结果：success, failed, partial
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Result { get; set; } = "success";

    /// <summary>
    ///     错误信息（如果操作失败）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    ///     导航属性 - 关联用户
    /// </summary>
    public virtual User User { get; set; } = null!;
}