using ClaudeCodeProxy.Core;
using ClaudeCodeProxy.Domain;
using ClaudeCodeProxy.Host.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaudeCodeProxy.Host.Services;

/// <summary>
///     钱包服务
/// </summary>
public class WalletService(IContext context)
{
    /// <summary>
    ///     获取用户钱包信息，如果不存在则创建
    /// </summary>
    public async Task<WalletDto> GetOrCreateWalletAsync(Guid userId)
    {
        var wallet = await context.Wallets
            .Include(w => w.User)
            .FirstOrDefaultAsync(w => w.UserId == userId);

        if (wallet == null)
        {
            wallet = new Wallet
            {
                UserId = userId,
                Balance = 0,
                TotalUsed = 0,
                TotalRecharged = 0,
                Status = "active"
            };

            context.Wallets.Add(wallet);
            await context.SaveAsync();

            // 重新查询以获取导航属性
            wallet = await context.Wallets
                .Include(w => w.User)
                .FirstAsync(w => w.UserId == userId);
        }

        return new WalletDto
        {
            Id = wallet.Id,
            UserId = wallet.UserId,
            Balance = wallet.Balance,
            TotalUsed = wallet.TotalUsed,
            TotalRecharged = wallet.TotalRecharged,
            Status = wallet.Status,
            LastUsedAt = wallet.LastUsedAt,
            LastRechargedAt = wallet.LastRechargedAt,
            UserName = wallet.User.Username,
            IsActive = wallet.Status == "active",
            CreatedAt = wallet.CreatedAt,
            ModifiedAt = wallet.ModifiedAt
        };
    }

    /// <summary>
    ///     充值钱包余额
    /// </summary>
    public async Task<bool> RechargeWalletAsync(Guid userId, decimal amount, string description = "钱包充值",
        string? paymentMethod = null, string? externalTransactionId = null)
    {
        if (amount <= 0) throw new ArgumentException("充值金额必须大于0");

        var wallet = await context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == userId);

        if (wallet == null)
        {
            wallet = new Wallet
            {
                UserId = userId,
                Balance = 0,
                TotalUsed = 0,
                TotalRecharged = 0,
                Status = "active"
            };
            context.Wallets.Add(wallet);
        }

        if (wallet.Status != "active") throw new InvalidOperationException("钱包状态异常，无法充值");

        var balanceBefore = wallet.Balance;
        wallet.AddBalance(amount, description);

        // 创建交易记录
        var transaction = new WalletTransaction
        {
            WalletId = wallet.Id,
            TransactionType = "recharge",
            Amount = amount,
            BalanceBefore = balanceBefore,
            BalanceAfter = wallet.Balance,
            Description = description,
            Status = "completed",
            PaymentMethod = paymentMethod,
            ExternalTransactionId = externalTransactionId
        };

        context.WalletTransactions.Add(transaction);
        await context.SaveAsync();

        return true;
    }

    /// <summary>
    ///     扣除钱包余额（用于API调用消费）
    /// </summary>
    public async Task<bool> DeductWalletAsync(Guid userId, decimal amount, string description = "API调用费用",
        Guid? requestLogId = null)
    {
        if (amount <= 0) return true; // 金额为0或负数时不需要扣费

        var wallet = await context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == userId);

        if (wallet == null || !wallet.HasSufficientBalance(amount)) return false; // 余额不足

        var balanceBefore = wallet.Balance;
        if (!wallet.DeductBalance(amount, description)) return false;

        // 创建交易记录
        var transaction = new WalletTransaction
        {
            WalletId = wallet.Id,
            TransactionType = "deduct",
            Amount = -amount, // 负数表示支出
            BalanceBefore = balanceBefore,
            BalanceAfter = wallet.Balance,
            Description = description,
            Status = "completed",
            RequestLogId = requestLogId
        };

        context.WalletTransactions.Add(transaction);
        await context.SaveAsync();

        return true;
    }

    /// <summary>
    ///     获取用户钱包交易记录
    /// </summary>
    public async Task<List<WalletTransactionDto>> GetWalletTransactionsAsync(Guid userId, int pageIndex = 0,
        int pageSize = 20, string? transactionType = null)
    {
        var wallet = await context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == userId);

        if (wallet == null) return new List<WalletTransactionDto>();

        var query = context.WalletTransactions
            .Where(t => t.WalletId == wallet.Id);

        if (!string.IsNullOrEmpty(transactionType)) query = query.Where(t => t.TransactionType == transactionType);

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .Select(t => new WalletTransactionDto
            {
                Id = t.Id,
                WalletId = t.WalletId,
                TransactionType = t.TransactionType,
                Amount = t.Amount,
                BalanceBefore = t.BalanceBefore,
                BalanceAfter = t.BalanceAfter,
                Description = t.Description,
                Status = t.Status,
                PaymentMethod = t.PaymentMethod,
                ExternalTransactionId = t.ExternalTransactionId,
                RequestLogId = t.RequestLogId,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();
    }

    /// <summary>
    ///     获取用户钱包统计信息
    /// </summary>
    public async Task<WalletStatisticsDto> GetWalletStatisticsAsync(Guid userId, int days = 30)
    {
        var wallet = await context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == userId);

        if (wallet == null)
            return new WalletStatisticsDto
            {
                UserId = userId,
                CurrentBalance = 0,
                TotalRecharged = 0,
                TotalUsed = 0,
                RecentTransactionCount = 0,
                DailyAverageUsage = 0
            };

        var startDate = DateTime.Now.AddDays(-days).Date;

        var recentTransactions = await context.WalletTransactions
            .Where(t => t.WalletId == wallet.Id && t.CreatedAt >= startDate)
            .ToListAsync();

        var totalRecentUsage = recentTransactions
            .Where(t => t.TransactionType == "deduct")
            .Sum(t => Math.Abs(t.Amount));

        var dailyAverageUsage = days > 0 ? totalRecentUsage / days : 0;

        return new WalletStatisticsDto
        {
            UserId = userId,
            CurrentBalance = wallet.Balance,
            TotalRecharged = wallet.TotalRecharged,
            TotalUsed = wallet.TotalUsed,
            RecentTransactionCount = recentTransactions.Count,
            DailyAverageUsage = dailyAverageUsage,
            LastUsedAt = wallet.LastUsedAt,
            LastRechargedAt = wallet.LastRechargedAt
        };
    }

    /// <summary>
    ///     检查用户是否有足够余额
    /// </summary>
    public async Task<bool> CheckSufficientBalanceAsync(Guid userId, decimal amount)
    {
        var wallet = await context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == userId);

        return wallet?.HasSufficientBalance(amount) ?? false;
    }

    /// <summary>
    ///     更新钱包状态
    /// </summary>
    public async Task<bool> UpdateWalletStatusAsync(Guid userId, string status)
    {
        var wallet = await context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == userId);

        if (wallet == null) return false;

        wallet.Status = status;
        await context.SaveAsync();

        return true;
    }

    /// <summary>
    ///     退款到钱包
    /// </summary>
    public async Task<bool> RefundToWalletAsync(Guid userId, decimal amount, string description = "退款",
        Guid? requestLogId = null)
    {
        if (amount <= 0) throw new ArgumentException("退款金额必须大于0");

        var wallet = await context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == userId);

        if (wallet == null)
        {
            wallet = new Wallet
            {
                UserId = userId,
                Balance = 0,
                TotalUsed = 0,
                TotalRecharged = 0,
                Status = "active"
            };
            context.Wallets.Add(wallet);
        }

        var balanceBefore = wallet.Balance;
        wallet.Balance += amount;

        // 创建交易记录
        var transaction = new WalletTransaction
        {
            WalletId = wallet.Id,
            TransactionType = "refund",
            Amount = amount,
            BalanceBefore = balanceBefore,
            BalanceAfter = wallet.Balance,
            Description = description,
            Status = "completed",
            RequestLogId = requestLogId
        };

        context.WalletTransactions.Add(transaction);
        await context.SaveAsync();

        return true;
    }
}