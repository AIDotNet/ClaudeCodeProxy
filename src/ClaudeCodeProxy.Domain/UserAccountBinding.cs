using System.ComponentModel.DataAnnotations;

namespace ClaudeCodeProxy.Domain;

/// <summary>
/// 用户账户绑定关系实体类
/// </summary>
public class UserAccountBinding : Entity<Guid>
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [Required]
    public Guid UserId { get; set; }
    
    /// <summary>
    /// 账户ID
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string AccountId { get; set; } = string.Empty;
    
    /// <summary>
    /// 绑定类型：private(私有), shared(共享)
    /// </summary>
    [Required]
    [MaxLength(20)]
    [RegularExpression("^(private|shared)$", ErrorMessage = "绑定类型只能是 'private' 或 'shared'")]
    public string BindingType { get; set; } = "private";
    
    /// <summary>
    /// 优先级 (1-100)，数字越小优先级越高
    /// </summary>
    [Range(1, 100, ErrorMessage = "优先级必须在1-100之间")]
    public int Priority { get; set; } = 50;
    
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// 绑定备注
    /// </summary>
    public string? Remarks { get; set; }
    
    /// <summary>
    /// 导航属性 - 关联用户
    /// </summary>
    public virtual User User { get; set; } = null!;
    
    /// <summary>
    /// 导航属性 - 关联账户
    /// </summary>
    public virtual Accounts Account { get; set; } = null!;
}