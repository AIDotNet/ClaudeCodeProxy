using ClaudeCodeProxy.Core;
using ClaudeCodeProxy.Host.Models;
using ClaudeCodeProxy.Host.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClaudeCodeProxy.Host.Endpoints;

/// <summary>
///     用户个人资料相关API端点
/// </summary>
public static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/profile")
            .WithTags("Profile")
            .RequireAuthorization();

        // 获取个人资料信息
        group.MapGet("/", GetProfile)
            .WithName("GetProfile")
            .WithSummary("获取个人资料信息")
            .WithOpenApi();

        // 更新个人资料
        group.MapPut("/", UpdateProfile)
            .WithName("UpdateProfile")
            .WithSummary("更新个人资料")
            .WithOpenApi();

        // 获取个人统计信息
        group.MapGet("/dashboard", GetProfileDashboard)
            .WithName("GetProfileDashboard")
            .WithSummary("获取个人仪表板信息")
            .WithOpenApi();
    }

    /// <summary>
    ///     获取个人资料信息
    /// </summary>
    private static async Task<IResult> GetProfile(
        IUserContext userContext,
        UserService userService,
        WalletService walletService)
    {
        var userId = userContext.GetCurrentUserId();
        if (!userId.HasValue) return Results.Unauthorized();

        try
        {
            var userInfo = await userService.GetUserByIdAsync(userId.Value);
            if (userInfo == null) return Results.NotFound("用户不存在");

            var wallet = await walletService.GetOrCreateWalletAsync(userId.Value);

            var profile = new UserProfileDto
            {
                Id = userInfo.Id,
                Username = userInfo.Username,
                Email = userInfo.Email,
                FirstName = userInfo.FirstName,
                LastName = userInfo.LastName,
                Avatar = userInfo.Avatar,
                IsActive = userInfo.IsActive,
                EmailConfirmed = userInfo.EmailConfirmed,
                LastLoginAt = userInfo.LastLoginAt,
                Provider = userInfo.Provider,
                RoleId = userInfo.RoleId,
                RoleName = userInfo.RoleName,
                CreatedAt = userInfo.CreatedAt,
                ModifiedAt = userInfo.ModifiedAt,
                Wallet = new WalletProfileDto
                {
                    Balance = wallet.Balance,
                    TotalUsed = wallet.TotalUsed,
                    TotalRecharged = wallet.TotalRecharged,
                    Status = wallet.Status,
                    LastUsedAt = wallet.LastUsedAt,
                    LastRechargedAt = wallet.LastRechargedAt,
                    IsActive = wallet.IsActive
                }
            };

            return Results.Ok(profile);
        }
        catch (Exception ex)
        {
            return Results.Problem($"获取个人资料失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     更新个人资料
    /// </summary>
    private static async Task<IResult> UpdateProfile(
        IUserContext userContext,
        UserService userService,
        [FromBody] UpdateProfileRequest request)
    {
        var userId = userContext.GetCurrentUserId();
        if (userId == null) return Results.Unauthorized();

        try
        {
            var updateRequest = new UpdateUserRequest
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Avatar = request.Avatar,
                IsActive = true, // 用户不能禁用自己
                EmailConfirmed = true, // 保持现有状态
                RoleId = 0 // 将被服务层忽略，用户不能修改自己的角色
            };

            // 获取当前用户信息以保持现有的角色和状态
            var currentUser = await userService.GetUserByIdAsync(userId.Value);
            if (currentUser == null) return Results.NotFound("用户不存在");

            updateRequest.IsActive = currentUser.IsActive;
            updateRequest.EmailConfirmed = currentUser.EmailConfirmed;
            updateRequest.RoleId = currentUser.RoleId;

            var updatedUser = await userService.UpdateUserAsync(userId.Value, updateRequest);
            if (updatedUser == null) return Results.NotFound("用户不存在");

            return Results.Ok(new { success = true, message = "个人资料更新成功", user = updatedUser });
        }
        catch (Exception ex)
        {
            return Results.Problem($"更新个人资料失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     修改密码
    /// </summary>
    private static async Task<IResult> ChangePassword(
        IUserContext userContext,
        UserService userService,
        [FromBody] ChangePasswordRequest request)
    {
        var userId = userContext.GetCurrentUserId();
        if (!userId.HasValue) return Results.Unauthorized();

        try
        {
            var success = await userService.ChangePasswordAsync(userId.Value, request);
            if (success) return Results.Ok(new { success = true, message = "密码修改成功" });

            return Results.NotFound(new { success = false, message = "用户不存在" });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.Problem($"修改密码失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     获取个人仪表板信息
    /// </summary>
    private static async Task<IResult> GetProfileDashboard(
        IUserContext userContext,
        WalletService walletService,
        RequestLogService requestLogService,
        ApiKeyService apiKeyService)
    {
        var userId = userContext.GetCurrentUserId();
        if (userId == null) return Results.Unauthorized();

        try
        {
            // 获取钱包统计
            var walletStats = await walletService.GetWalletStatisticsAsync(userId.Value);

            // 获取请求统计（最近30天）
            var requestStats = await requestLogService.GetUserRequestStatisticsAsync(userId.Value);

            // 获取API Key数量
            var apiKeys = await apiKeyService.GetUserApiKeysAsync(userId.Value);
            var activeApiKeys = apiKeys.Count(k => k.IsEnabled);

            var dashboard = new ProfileDashboardDto
            {
                UserId = userId.Value,
                Wallet = new WalletStatisticsDto
                {
                    UserId = walletStats.UserId,
                    CurrentBalance = walletStats.CurrentBalance,
                    TotalRecharged = walletStats.TotalRecharged,
                    TotalUsed = walletStats.TotalUsed,
                    RecentTransactionCount = walletStats.RecentTransactionCount,
                    DailyAverageUsage = walletStats.DailyAverageUsage,
                    LastUsedAt = walletStats.LastUsedAt,
                    LastRechargedAt = walletStats.LastRechargedAt
                },
                Requests = requestStats,
                ApiKeyCount = apiKeys.Count,
                ActiveApiKeyCount = activeApiKeys,
                LastUpdateTime = DateTime.Now
            };

            return Results.Ok(dashboard);
        }
        catch (Exception ex)
        {
            return Results.Problem($"获取仪表板信息失败: {ex.Message}");
        }
    }
}