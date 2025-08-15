namespace ClaudeCodeProxy.Domain;

/// <summary>
/// 用户账户绑定关系实体类
/// </summary>
public class UserAccountBinding : Entity<Guid>
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// 账户ID
    /// </summary>
    public string AccountId { get; set; } = string.Empty;
    
    /// <summary>
    /// 绑定类型：private(私有), shared(共享)
    /// </summary>
    public string BindingType { get; set; } = "private";
    
    /// <summary>
    /// 优先级 (1-100)，数字越小优先级越高
    /// </summary>
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