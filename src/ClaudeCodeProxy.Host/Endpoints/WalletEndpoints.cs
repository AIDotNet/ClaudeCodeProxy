using ClaudeCodeProxy.Core;
using ClaudeCodeProxy.Host.Models;
using ClaudeCodeProxy.Host.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClaudeCodeProxy.Host.Endpoints;

/// <summary>
///     钱包相关API端点
/// </summary>
public static class WalletEndpoints
{
    public static void MapWalletEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/wallet")
            .WithTags("Wallet")
            .RequireAuthorization();

        // 获取当前用户钱包信息
        group.MapGet("/", GetUserWallet)
            .WithName("GetUserWallet")
            .WithSummary("获取当前用户钱包信息")
            .WithOpenApi();

        // 获取用户钱包交易记录
        group.MapGet("/transactions", GetWalletTransactions)
            .WithName("GetWalletTransactions")
            .WithSummary("获取用户钱包交易记录")
            .WithOpenApi();

        // 获取钱包统计信息
        group.MapGet("/statistics", GetWalletStatistics)
            .WithName("GetWalletStatistics")
            .WithSummary("获取钱包统计信息")
            .WithOpenApi();

        // 充值钱包（管理员功能）
        group.MapPost("/recharge", RechargeWallet)
            .WithName("RechargeWallet")
            .WithSummary("充值钱包余额")
            .WithOpenApi()
            .RequireAuthorization("Admin");

        // 管理员获取指定用户钱包信息
        group.MapGet("/{userId:Guid}", GetUserWalletByAdmin)
            .WithName("GetUserWalletByAdmin")
            .WithSummary("管理员获取指定用户钱包信息")
            .WithOpenApi()
            .RequireAuthorization("Admin");

        // 管理员为指定用户充值
        group.MapPost("/{userId:Guid}/recharge", RechargeUserWalletByAdmin)
            .WithName("RechargeUserWalletByAdmin")
            .WithSummary("管理员为指定用户充值")
            .WithOpenApi()
            .RequireAuthorization("Admin");

        // 管理员更新钱包状态
        group.MapPut("/{userId:Guid}/status", UpdateWalletStatus)
            .WithName("UpdateWalletStatus")
            .WithSummary("更新钱包状态")
            .WithOpenApi()
            .RequireAuthorization("Admin");

        // 获取指定用户钱包交易记录（管理员）
        group.MapGet("/{userId:Guid}/transactions", GetUserWalletTransactionsByAdmin)
            .WithName("GetUserWalletTransactionsByAdmin")
            .WithSummary("管理员获取指定用户钱包交易记录")
            .WithOpenApi()
            .RequireAuthorization("Admin");
    }

    /// <summary>
    ///     获取当前用户钱包信息
    /// </summary>
    private static async Task<IResult> GetUserWallet(
        IUserContext userContext,
        WalletService walletService)
    {
        var userId = userContext.GetCurrentUserId();
        if (userId == null) return Results.Unauthorized();

        try
        {
            var wallet = await walletService.GetOrCreateWalletAsync(userId.Value);
            return Results.Ok(wallet);
        }
        catch (Exception ex)
        {
            return Results.Problem($"获取钱包信息失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     获取用户钱包交易记录
    /// </summary>
    private static async Task<IResult> GetWalletTransactions(
        IUserContext userContext,
        WalletService walletService,
        int pageIndex = 0,
        int pageSize = 20,
        string? transactionType = null)
    {
        var userId = userContext.GetCurrentUserId();
        if (userId == null) return Results.Unauthorized();

        try
        {
            var transactions =
                await walletService.GetWalletTransactionsAsync(userId.Value, pageIndex, pageSize, transactionType);
            return Results.Ok(transactions);
        }
        catch (Exception ex)
        {
            return Results.Problem($"获取交易记录失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     获取钱包统计信息
    /// </summary>
    private static async Task<IResult> GetWalletStatistics(
        IUserContext userContext,
        WalletService walletService,
        int days = 30)
    {
        var userId = userContext.GetCurrentUserId();
        if (userId == null) return Results.Unauthorized();

        try
        {
            var statistics = await walletService.GetWalletStatisticsAsync(userId.Value, days);
            return Results.Ok(statistics);
        }
        catch (Exception ex)
        {
            return Results.Problem($"获取统计信息失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     充值钱包（当前用户）
    /// </summary>
    private static async Task<IResult> RechargeWallet(
        IUserContext userContext,
        WalletService walletService,
        [FromBody] WalletRechargeRequest request)
    {
        var userId = userContext.GetCurrentUserId();
        if (userId == null) return Results.Unauthorized();

        try
        {
            var success = await walletService.RechargeWalletAsync(
                userId.Value,
                request.Amount,
                request.Description,
                request.PaymentMethod,
                request.ExternalTransactionId);

            if (success) return Results.Ok(new { success = true, message = "充值成功" });

            return Results.BadRequest(new { success = false, message = "充值失败" });
        }
        catch (Exception ex)
        {
            return Results.Problem($"充值失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     管理员获取指定用户钱包信息
    /// </summary>
    private static async Task<IResult> GetUserWalletByAdmin(
        Guid userId,
        WalletService walletService)
    {
        try
        {
            var wallet = await walletService.GetOrCreateWalletAsync(userId);
            return Results.Ok(wallet);
        }
        catch (Exception ex)
        {
            return Results.Problem($"获取钱包信息失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     管理员为指定用户充值
    /// </summary>
    private static async Task<IResult> RechargeUserWalletByAdmin(
        Guid userId,
        WalletService walletService,
        [FromBody] WalletRechargeRequest request)
    {
        try
        {
            var success = await walletService.RechargeWalletAsync(
                userId,
                request.Amount,
                request.Description,
                request.PaymentMethod,
                request.ExternalTransactionId);

            if (success) return Results.Ok(new { success = true, message = "充值成功" });

            return Results.BadRequest(new { success = false, message = "充值失败" });
        }
        catch (Exception ex)
        {
            return Results.Problem($"充值失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     更新钱包状态
    /// </summary>
    private static async Task<IResult> UpdateWalletStatus(
        Guid userId,
        WalletService walletService,
        [FromBody] WalletStatusUpdateRequest request)
    {
        try
        {
            var success = await walletService.UpdateWalletStatusAsync(userId, request.Status);

            if (success) return Results.Ok(new { success = true, message = "状态更新成功" });

            return Results.NotFound(new { success = false, message = "用户钱包不存在" });
        }
        catch (Exception ex)
        {
            return Results.Problem($"更新状态失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     管理员获取指定用户钱包交易记录
    /// </summary>
    private static async Task<IResult> GetUserWalletTransactionsByAdmin(
        Guid userId,
        WalletService walletService,
        int pageIndex = 0,
        int pageSize = 20,
        string? transactionType = null)
    {
        try
        {
            var transactions =
                await walletService.GetWalletTransactionsAsync(userId, pageIndex, pageSize, transactionType);
            return Results.Ok(transactions);
        }
        catch (Exception ex)
        {
            return Results.Problem($"获取交易记录失败: {ex.Message}");
        }
    }
}