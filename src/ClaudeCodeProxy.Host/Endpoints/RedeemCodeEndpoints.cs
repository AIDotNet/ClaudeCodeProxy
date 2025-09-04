using ClaudeCodeProxy.Host.Models;
using ClaudeCodeProxy.Host.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ClaudeCodeProxy.Abstraction;
using ClaudeCodeProxy.Core;

namespace ClaudeCodeProxy.Host.Endpoints;

/// <summary>
/// 兑换码相关API端点
/// </summary>
public static class RedeemCodeEndpoints
{
    public static void MapRedeemCodeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/redeem-codes")
            .WithTags("RedeemCode")
            .RequireAuthorization();

        // 用户使用兑换码
        group.MapPost("/use", UseRedeemCode)
            .WithName("UseRedeemCode")
            .WithSummary("使用兑换码")
            .WithOpenApi();

        // 获取用户兑换记录
        group.MapGet("/my-records", GetMyRedeemRecords)
            .WithName("GetMyRedeemRecords")
            .WithSummary("获取我的兑换记录")
            .WithOpenApi();

        // 管理员API组
        var adminGroup = app.MapGroup("/api/admin/redeem-codes")
            .WithTags("Admin-RedeemCode")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        // 创建兑换码
        adminGroup.MapPost("/", CreateRedeemCodes)
            .WithName("CreateRedeemCodes")
            .WithSummary("创建兑换码")
            .WithOpenApi();

        // 获取兑换码列表
        adminGroup.MapPost("/list", GetRedeemCodeList)
            .WithName("GetRedeemCodeList")
            .WithSummary("获取兑换码列表")
            .WithOpenApi();

        // 启用/禁用兑换码
        adminGroup.MapPut("/{id}/status", UpdateRedeemCodeStatus)
            .WithName("UpdateRedeemCodeStatus")
            .WithSummary("更新兑换码状态")
            .WithOpenApi();

        // 删除兑换码
        adminGroup.MapDelete("/{id}", DeleteRedeemCode)
            .WithName("DeleteRedeemCode")
            .WithSummary("删除兑换码")
            .WithOpenApi();

        // 获取兑换码统计
        adminGroup.MapGet("/stats", GetRedeemCodeStats)
            .WithName("GetRedeemCodeStats")
            .WithSummary("获取兑换码统计")
            .WithOpenApi();
    }

    /// <summary>
    /// 使用兑换码
    /// </summary>
    private static async Task<IResult> UseRedeemCode(
        IUserContext userContext,
        RedeemCodeService redeemCodeService,
        [FromBody] UseRedeemCodeRequest request)
    {
        var userId = userContext.GetCurrentUserId();
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return Results.BadRequest(new { success = false, message = "兑换码不能为空" });
        }

        try
        {
            var result = await redeemCodeService.UseRedeemCodeAsync(request.Code.Trim().ToUpper(), userId.Value);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.Problem($"使用兑换码失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取用户兑换记录
    /// </summary>
    private static async Task<IResult> GetMyRedeemRecords(
        IUserContext userContext,
        RedeemCodeService redeemCodeService,
        int page = 1,
        int pageSize = 20)
    {
        var userId = userContext.GetCurrentUserId();
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var records = await redeemCodeService.GetUserRedeemRecordsAsync(
                userId.Value,
                page - 1,
                pageSize);

            return Results.Ok(new { success = true, data = records });
        }
        catch (Exception ex)
        {
            return Results.Problem($"获取兑换记录失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 创建兑换码（管理员）
    /// </summary>
    private static async Task<IResult> CreateRedeemCodes(
        IUserContext userContext,
        RedeemCodeService redeemCodeService,
        [FromBody] CreateRedeemCodeRequest request)
    {
        var userId = userContext.GetCurrentUserId();
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        if (request.Amount <= 0)
        {
            return Results.BadRequest(new { success = false, message = "金额必须大于0" });
        }

        if (request.Count <= 0 || request.Count > 100)
        {
            return Results.BadRequest(new { success = false, message = "生成数量必须在1-100之间" });
        }

        try
        {
            var redeemCodes = await redeemCodeService.CreateRedeemCodesAsync(request, userId.Value);
            return Results.Ok(new { success = true, data = redeemCodes, message = $"成功创建 {redeemCodes.Count} 个兑换码" });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.Problem($"创建兑换码失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取兑换码列表（管理员）
    /// </summary>
    private static async Task<IResult> GetRedeemCodeList(
        RedeemCodeService redeemCodeService,
        [FromBody] RedeemCodeListRequest request)
    {
        try
        {
            var result = await redeemCodeService.GetRedeemCodesAsync(request);
            return Results.Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return Results.Problem($"获取兑换码列表失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新兑换码状态（管理员）
    /// </summary>
    private static async Task<IResult> UpdateRedeemCodeStatus(
        RedeemCodeService redeemCodeService,
        Guid id,
        [FromBody] object requestBody)
    {
        try
        {
            // 从请求体中提取isEnabled
            var requestDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                System.Text.Json.JsonSerializer.Serialize(requestBody, ThorJsonSerializer.DefaultOptions));

            if (!requestDict.TryGetValue("isEnabled", out var isEnabledObj) ||
                !bool.TryParse(isEnabledObj.ToString(), out var isEnabled))
            {
                return Results.BadRequest(new { success = false, message = "无效的请求参数" });
            }

            var success = await redeemCodeService.UpdateRedeemCodeStatusAsync(id, isEnabled);

            if (success)
            {
                return Results.Ok(new { success = true, message = $"兑换码已{(isEnabled ? "启用" : "禁用")}" });
            }
            else
            {
                return Results.NotFound(new { success = false, message = "兑换码不存在" });
            }
        }
        catch (Exception ex)
        {
            return Results.Problem($"更新兑换码状态失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 删除兑换码（管理员）
    /// </summary>
    private static async Task<IResult> DeleteRedeemCode(
        RedeemCodeService redeemCodeService,
        Guid id)
    {
        try
        {
            var success = await redeemCodeService.DeleteRedeemCodeAsync(id);

            if (success)
            {
                return Results.Ok(new { success = true, message = "兑换码删除成功" });
            }
            else
            {
                return Results.NotFound(new { success = false, message = "兑换码不存在" });
            }
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.Problem($"删除兑换码失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取兑换码统计（管理员）
    /// </summary>
    private static async Task<IResult> GetRedeemCodeStats(
        RedeemCodeService redeemCodeService)
    {
        try
        {
            var stats = await redeemCodeService.GetRedeemCodeStatsAsync();
            return Results.Ok(new { success = true, data = stats });
        }
        catch (Exception ex)
        {
            return Results.Problem($"获取兑换码统计失败: {ex.Message}");
        }
    }
}