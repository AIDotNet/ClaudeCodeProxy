namespace ClaudeCodeProxy.Host.Models;

/// <summary>
///     钱包DTO
/// </summary>
public class WalletDto
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public decimal Balance { get; set; }
    public decimal TotalUsed { get; set; }
    public decimal TotalRecharged { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? LastUsedAt { get; set; }
    public DateTime? LastRechargedAt { get; set; }
    public string UserName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

/// <summary>
///     钱包交易记录DTO
/// </summary>
public class WalletTransactionDto
{
    public int Id { get; set; }
    public int WalletId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public string? ExternalTransactionId { get; set; }
    public Guid? RequestLogId { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
///     钱包统计信息DTO
/// </summary>
public class WalletStatisticsDto
{
    public Guid UserId { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal TotalRecharged { get; set; }
    public decimal TotalUsed { get; set; }
    public int RecentTransactionCount { get; set; }
    public decimal DailyAverageUsage { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? LastRechargedAt { get; set; }
}

/// <summary>
///     钱包充值请求
/// </summary>
public class WalletRechargeRequest
{
    public decimal Amount { get; set; }
    public string Description { get; set; } = "钱包充值";
    public string? PaymentMethod { get; set; }
    public string? ExternalTransactionId { get; set; }
}

/// <summary>
///     钱包状态更新请求
/// </summary>
public class WalletStatusUpdateRequest
{
    public string Status { get; set; } = string.Empty;
}