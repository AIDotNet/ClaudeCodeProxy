using System.ComponentModel.DataAnnotations;

namespace ClaudeCodeProxy.Domain;

/// <summary>
///     用户钱包实体类
///     每个用户拥有一个钱包，用于管理余额和充值记录
/// </summary>
public class Wallet : Entity<int>
{
    /// <summary>
    ///     用户ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    ///     当前余额（美元）
    /// </summary>
    public decimal Balance { get; set; }

    /// <summary>
    ///     已使用金额（美元）
    /// </summary>
    public decimal TotalUsed { get; set; }

    /// <summary>
    ///     总充值金额（美元）
    /// </summary>
    public decimal TotalRecharged { get; set; }

    /// <summary>
    ///     钱包状态：active, frozen, suspended
    /// </summary>
    [MaxLength(20)]
    public string Status { get; set; } = "active";

    /// <summary>
    ///     最后使用时间
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    ///     最后充值时间
    /// </summary>
    public DateTime? LastRechargedAt { get; set; }

    /// <summary>
    ///     导航属性 - 关联用户
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    ///     导航属性 - 钱包交易记录
    /// </summary>
    public virtual ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();

    /// <summary>
    ///     检查余额是否充足
    /// </summary>
    /// <param name="amount">需要扣除的金额</param>
    /// <returns>是否充足</returns>
    public bool HasSufficientBalance(decimal amount)
    {
        return Status == "active" && Balance >= amount;
    }

    /// <summary>
    ///     扣除余额
    /// </summary>
    /// <param name="amount">扣除金额</param>
    /// <param name="description">扣除说明</param>
    /// <returns>是否成功</returns>
    public bool DeductBalance(decimal amount, string description = "API调用费用")
    {
        if (!HasSufficientBalance(amount)) return false;

        Balance -= amount;
        TotalUsed += amount;
        LastUsedAt = DateTime.Now;

        return true;
    }

    /// <summary>
    ///     充值余额
    /// </summary>
    /// <param name="amount">充值金额</param>
    /// <param name="description">充值说明</param>
    public void AddBalance(decimal amount, string description = "钱包充值")
    {
        Balance += amount;
        TotalRecharged += amount;
        LastRechargedAt = DateTime.Now;
    }

    /// <summary>
    ///     获取钱包概览信息
    /// </summary>
    /// <returns>钱包概览</returns>
    public WalletSummary GetSummary()
    {
        return new WalletSummary
        {
            UserId = UserId,
            Balance = Balance,
            TotalUsed = TotalUsed,
            TotalRecharged = TotalRecharged,
            Status = Status,
            LastUsedAt = LastUsedAt,
            LastRechargedAt = LastRechargedAt,
            IsActive = Status == "active"
        };
    }
}

/// <summary>
///     钱包交易记录实体类
/// </summary>
public class WalletTransaction : Entity<int>
{
    /// <summary>
    ///     钱包ID
    /// </summary>
    public int WalletId { get; set; }

    /// <summary>
    ///     交易类型：recharge（充值）, deduct（扣除）, refund（退款）
    /// </summary>
    [MaxLength(20)]
    public string TransactionType { get; set; } = string.Empty;

    /// <summary>
    ///     交易金额（正数为收入，负数为支出）
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    ///     交易前余额
    /// </summary>
    public decimal BalanceBefore { get; set; }

    /// <summary>
    ///     交易后余额
    /// </summary>
    public decimal BalanceAfter { get; set; }

    /// <summary>
    ///     交易描述
    /// </summary>
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     关联的请求日志ID（如果是API调用扣费）
    /// </summary>
    public Guid? RequestLogId { get; set; }

    /// <summary>
    ///     交易状态：pending, completed, failed
    /// </summary>
    [MaxLength(20)]
    public string Status { get; set; } = "completed";

    /// <summary>
    ///     支付方式（充值时使用）：manual, credit_card, paypal, alipay, wechat
    /// </summary>
    [MaxLength(50)]
    public string? PaymentMethod { get; set; }

    /// <summary>
    ///     外部交易ID（第三方支付订单号）
    /// </summary>
    [MaxLength(100)]
    public string? ExternalTransactionId { get; set; }

    /// <summary>
    ///     导航属性 - 关联钱包
    /// </summary>
    public virtual Wallet Wallet { get; set; } = null!;

    /// <summary>
    ///     导航属性 - 关联请求日志
    /// </summary>
    public virtual RequestLog? RequestLog { get; set; }
}

/// <summary>
///     钱包概览信息
/// </summary>
public class WalletSummary
{
    public Guid UserId { get; set; }
    public decimal Balance { get; set; }
    public decimal TotalUsed { get; set; }
    public decimal TotalRecharged { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? LastUsedAt { get; set; }
    public DateTime? LastRechargedAt { get; set; }
    public bool IsActive { get; set; }
}