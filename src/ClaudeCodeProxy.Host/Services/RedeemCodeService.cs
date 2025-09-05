using System.Security.Cryptography;
using System.Text;
using ClaudeCodeProxy.Core;
using ClaudeCodeProxy.Domain;
using ClaudeCodeProxy.Host.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaudeCodeProxy.Host.Services;

/// <summary>
///     兑换码服务
/// </summary>
public class RedeemCodeService(IContext context, WalletService walletService)
{
    /// <summary>
    ///     生成兑换码
    /// </summary>
    public async Task<List<RedeemCodeDto>> CreateRedeemCodesAsync(
        CreateRedeemCodeRequest request,
        Guid createdByUserId,
        CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0) throw new ArgumentException("兑换金额必须大于0");

        if (request.Count <= 0 || request.Count > 100) throw new ArgumentException("生成数量必须在1-100之间");

        var redeemCodes = new List<RedeemCode>();
        var redeemCodeDtos = new List<RedeemCodeDto>();

        for (var i = 0; i < request.Count; i++)
        {
            var code = GenerateRedeemCode();
            var redeemCode = new RedeemCode
            {
                Code = code,
                Type = request.Type,
                Amount = request.Amount,
                Description = request.Description,
                ExpiresAt = request.ExpiresAt,
                CreatedByUserId = createdByUserId
            };

            redeemCodes.Add(redeemCode);
        }

        context.RedeemCodes.AddRange(redeemCodes);
        await context.SaveAsync(cancellationToken);

        // 返回创建的兑换码信息
        foreach (var redeemCode in redeemCodes) redeemCodeDtos.Add(MapToDto(redeemCode));

        return redeemCodeDtos;
    }

    /// <summary>
    ///     使用兑换码
    /// </summary>
    public async Task<RedeemCodeUseResult> UseRedeemCodeAsync(
        string code,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var redeemCode = await context.RedeemCodes
            .Include(r => r.UsedByUser)
            .FirstOrDefaultAsync(r => r.Code == code, cancellationToken);

        if (redeemCode == null)
            return new RedeemCodeUseResult
            {
                Success = false,
                Message = "兑换码不存在"
            };

        if (!redeemCode.IsValid())
        {
            var message = redeemCode.IsUsed ? "兑换码已被使用" :
                !redeemCode.IsEnabled ? "兑换码已被禁用" :
                "兑换码已过期";

            return new RedeemCodeUseResult
            {
                Success = false,
                Message = message
            };
        }

        // 检查是否已被当前用户使用
        if (redeemCode.UsedByUserId == userId)
            return new RedeemCodeUseResult
            {
                Success = false,
                Message = "您已经使用过此兑换码"
            };

        // 使用兑换码
        redeemCode.Use(userId);

        // 根据类型处理奖励
        decimal newBalance = 0;
        if (redeemCode.Type == "balance")
        {
            // 充值到钱包
            await walletService.RechargeWalletAsync(
                userId,
                redeemCode.Amount,
                $"兑换码充值 - {redeemCode.Code}",
                "redeem_code",
                redeemCode.Id.ToString());

            // 获取新余额
            var wallet = await walletService.GetOrCreateWalletAsync(userId);
            newBalance = wallet.Balance;
        }

        await context.SaveAsync(cancellationToken);

        return new RedeemCodeUseResult
        {
            Success = true,
            Message = "兑换成功",
            Amount = redeemCode.Amount,
            Type = redeemCode.Type,
            NewBalance = newBalance
        };
    }

    /// <summary>
    ///     获取兑换码列表（管理员）
    /// </summary>
    public async Task<RedeemCodeListResponse> GetRedeemCodesAsync(
        RedeemCodeListRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = context.RedeemCodes
            .Include(r => r.CreatedByUser)
            .Include(r => r.UsedByUser)
            .AsQueryable();

        // 应用过滤条件
        if (!string.IsNullOrEmpty(request.Code)) query = query.Where(r => r.Code.Contains(request.Code));

        if (!string.IsNullOrEmpty(request.Type)) query = query.Where(r => r.Type == request.Type);

        if (request.IsUsed.HasValue) query = query.Where(r => r.IsUsed == request.IsUsed.Value);

        if (request.IsEnabled.HasValue) query = query.Where(r => r.IsEnabled == request.IsEnabled.Value);

        if (request.CreatedByUserId.HasValue)
            query = query.Where(r => r.CreatedByUserId == request.CreatedByUserId.Value);

        if (request.UsedByUserId.HasValue) query = query.Where(r => r.UsedByUserId == request.UsedByUserId.Value);

        if (request.StartDate.HasValue) query = query.Where(r => r.CreatedAt >= request.StartDate.Value);

        if (request.EndDate.HasValue) query = query.Where(r => r.CreatedAt <= request.EndDate.Value);

        // 计算总数
        var total = await query.CountAsync(cancellationToken);

        // 应用排序
        query = request.SortBy?.ToLower() switch
        {
            "code" => request.SortDirection == "desc"
                ? query.OrderByDescending(r => r.Code)
                : query.OrderBy(r => r.Code),
            "amount" => request.SortDirection == "desc"
                ? query.OrderByDescending(r => r.Amount)
                : query.OrderBy(r => r.Amount),
            "usedat" => request.SortDirection == "desc"
                ? query.OrderByDescending(r => r.UsedAt)
                : query.OrderBy(r => r.UsedAt),
            "expiresat" => request.SortDirection == "desc"
                ? query.OrderByDescending(r => r.ExpiresAt)
                : query.OrderBy(r => r.ExpiresAt),
            _ => request.SortDirection == "desc"
                ? query.OrderByDescending(r => r.CreatedAt)
                : query.OrderBy(r => r.CreatedAt)
        };

        // 应用分页
        var redeemCodes = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = redeemCodes.Select(MapToDto).ToList();

        return new RedeemCodeListResponse
        {
            Data = dtos,
            Total = total,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling((double)total / request.PageSize)
        };
    }

    /// <summary>
    ///     获取用户兑换记录
    /// </summary>
    public async Task<List<RedeemRecordDto>> GetUserRedeemRecordsAsync(
        Guid userId,
        int pageIndex = 0,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return await context.RedeemCodes
            .Where(r => r.UsedByUserId == userId && r.IsUsed)
            .OrderByDescending(r => r.UsedAt)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .Select(r => new RedeemRecordDto
            {
                Id = r.Id,
                Code = r.Code,
                Type = r.Type,
                Amount = r.Amount,
                Description = r.Description ?? "",
                UsedAt = r.UsedAt!.Value
            })
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    ///     禁用/启用兑换码
    /// </summary>
    public async Task<bool> UpdateRedeemCodeStatusAsync(
        Guid id,
        bool isEnabled,
        CancellationToken cancellationToken = default)
    {
        var redeemCode = await context.RedeemCodes
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (redeemCode == null) return false;

        redeemCode.IsEnabled = isEnabled;
        await context.SaveAsync(cancellationToken);

        return true;
    }

    /// <summary>
    ///     删除兑换码
    /// </summary>
    public async Task<bool> DeleteRedeemCodeAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var redeemCode = await context.RedeemCodes
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (redeemCode == null) return false;

        if (redeemCode.IsUsed) throw new InvalidOperationException("已使用的兑换码不能删除");

        context.RedeemCodes.Remove(redeemCode);
        await context.SaveAsync(cancellationToken);

        return true;
    }

    /// <summary>
    ///     获取兑换码统计信息
    /// </summary>
    public async Task<object> GetRedeemCodeStatsAsync(CancellationToken cancellationToken = default)
    {
        var totalCodes = await context.RedeemCodes.CountAsync(cancellationToken);
        var usedCodes = await context.RedeemCodes.CountAsync(r => r.IsUsed, cancellationToken);
        var expiredCodes = await context.RedeemCodes
            .CountAsync(r => r.ExpiresAt != null && r.ExpiresAt < DateTime.Now, cancellationToken);
        var totalAmount = await context.RedeemCodes
            .Where(r => r.IsUsed)
            .SumAsync(r => r.Amount, cancellationToken);

        return new
        {
            TotalCodes = totalCodes,
            UsedCodes = usedCodes,
            UnusedCodes = totalCodes - usedCodes,
            ExpiredCodes = expiredCodes,
            TotalRedeemedAmount = totalAmount,
            UsageRate = totalCodes > 0 ? (double)usedCodes / totalCodes * 100 : 0
        };
    }

    /// <summary>
    ///     生成兑换码
    /// </summary>
    private static string GenerateRedeemCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var result = new StringBuilder();

        using var rng = RandomNumberGenerator.Create();
        var buffer = new byte[16];
        rng.GetBytes(buffer);

        for (var i = 0; i < 16; i++)
        {
            if (i > 0 && i % 4 == 0) result.Append('-');
            result.Append(chars[buffer[i] % chars.Length]);
        }

        return result.ToString();
    }

    /// <summary>
    ///     映射到DTO
    /// </summary>
    private static RedeemCodeDto MapToDto(RedeemCode redeemCode)
    {
        return new RedeemCodeDto
        {
            Id = redeemCode.Id,
            Code = redeemCode.Code,
            Type = redeemCode.Type,
            Amount = redeemCode.Amount,
            Description = redeemCode.Description,
            IsUsed = redeemCode.IsUsed,
            UsedByUserId = redeemCode.UsedByUserId,
            UsedByUserName = redeemCode.UsedByUser?.Username,
            UsedAt = redeemCode.UsedAt,
            ExpiresAt = redeemCode.ExpiresAt,
            IsEnabled = redeemCode.IsEnabled,
            CreatedByUserId = redeemCode.CreatedByUserId,
            CreatedByUserName = redeemCode.CreatedByUser?.Username ?? "",
            CreatedAt = redeemCode.CreatedAt,
            ModifiedAt = redeemCode.ModifiedAt
        };
    }
}