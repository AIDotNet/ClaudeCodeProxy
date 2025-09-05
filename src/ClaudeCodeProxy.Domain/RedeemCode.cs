namespace ClaudeCodeProxy.Domain;

/// <summary>
///     兑换码实体
/// </summary>
public class RedeemCode : Entity<string>
{
    /// <summary>
    ///     兑换码ID
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    ///     兑换码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    ///     兑换码类型 (balance: 余额充值, credits: 积分奖励)
    /// </summary>
    public string Type { get; set; } = "balance";

    /// <summary>
    ///     兑换金额/积分
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    ///     兑换码描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     是否已使用
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    ///     使用用户ID
    /// </summary>
    public Guid? UsedByUserId { get; set; }

    /// <summary>
    ///     使用时间
    /// </summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>
    ///     过期时间
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    ///     是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    ///     创建者用户ID
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    ///     使用用户导航属性
    /// </summary>
    public User? UsedByUser { get; set; }

    /// <summary>
    ///     创建者用户导航属性
    /// </summary>
    public User CreatedByUser { get; set; } = null!;

    /// <summary>
    ///     检查兑换码是否有效
    /// </summary>
    public bool IsValid()
    {
        return IsEnabled &&
               !IsUsed &&
               (ExpiresAt == null || ExpiresAt > DateTime.Now);
    }

    /// <summary>
    ///     使用兑换码
    /// </summary>
    public void Use(Guid userId)
    {
        if (!IsValid()) throw new InvalidOperationException("兑换码无效或已过期");

        IsUsed = true;
        UsedByUserId = userId;
        UsedAt = DateTime.Now;
    }
}