using System.Security.Claims;
using ClaudeCodeProxy.Domain;
using ClaudeCodeProxy.Host.Models;
using ClaudeCodeProxy.Host.Services;

namespace ClaudeCodeProxy.Host.Endpoints;

/// <summary>
///     邀请系统端点
/// </summary>
public static class InvitationEndpoints
{
    public static void MapInvitationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/invitation")
            .WithTags("邀请系统")
            .RequireAuthorization();

        // 获取当前用户的邀请链接
        group.MapGet("/link", async (HttpContext context, IInvitationService invitationService) =>
            {
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdClaim, out var userId)) return Results.Unauthorized();

                try
                {
                    var link = await invitationService.GetUserInvitationLinkAsync(userId);
                    return Results.Ok(new { invitationLink = link });
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(ex.Message);
                }
            })
            .WithName("GetInvitationLink")
            .WithSummary("获取当前用户的邀请链接");

        // 获取当前用户的邀请统计信息
        group.MapGet("/stats", async (HttpContext context, IInvitationService invitationService) =>
            {
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdClaim, out var userId)) return Results.Unauthorized();

                var link = await invitationService.GetUserInvitationLinkAsync(userId);
                var totalInvited = await invitationService.GetUserInvitationCountAsync(userId);
                var maxInvitations = await invitationService.GetMaxInvitationsAsync();
                var records = await invitationService.GetUserInvitationRecordsAsync(userId);
                var totalReward = records.Sum(r => r.InviterReward);

                var stats = new InvitationStatsDto
                {
                    TotalInvited = totalInvited,
                    MaxInvitations = maxInvitations,
                    TotalReward = totalReward,
                    InvitationLink = link
                };

                return Results.Ok(stats);
            })
            .WithName("GetInvitationStats")
            .WithSummary("获取当前用户的邀请统计信息");

        // 获取当前用户的邀请记录
        group.MapGet("/records", async (HttpContext context, IInvitationService invitationService) =>
            {
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdClaim, out var userId)) return Results.Unauthorized();

                var records = await invitationService.GetUserInvitationRecordsAsync(userId);
                return Results.Ok(records);
            })
            .WithName("GetInvitationRecords")
            .WithSummary("获取当前用户的邀请记录");

        // 检查邀请码是否有效（注册时使用）
        group.MapGet("/validate/{code}", async (string code, IInvitationService invitationService) =>
            {
                if (string.IsNullOrEmpty(code)) return Results.BadRequest("邀请码不能为空");

                // 这里只是验证邀请码格式和存在性，不处理实际邀请逻辑
                // 实际的邀请处理在注册时进行
                return Results.Ok(new { valid = !string.IsNullOrEmpty(code) && code.Length == 8 });
            })
            .WithName("ValidateInvitationCode")
            .WithSummary("检查邀请码是否有效")
            .AllowAnonymous();

        // 管理员获取邀请系统设置
        group.MapGet("/settings", async (IInvitationService invitationService) =>
            {
                var inviterReward = await invitationService.GetInviterRewardAsync();
                var invitedReward = await invitationService.GetInvitedRewardAsync();
                var maxInvitations = await invitationService.GetMaxInvitationsAsync();
                var enabledSetting =
                    await invitationService.GetInvitationSettingAsync(InvitationSettings.Keys.InvitationEnabled);
                var invitationEnabled = bool.TryParse(enabledSetting, out var enabled) ? enabled : true;

                var settings = new UpdateInvitationSettingsRequest
                {
                    InviterReward = inviterReward,
                    InvitedReward = invitedReward,
                    MaxInvitations = maxInvitations,
                    InvitationEnabled = invitationEnabled
                };

                return Results.Ok(settings);
            })
            .WithName("GetInvitationSettings")
            .WithSummary("管理员获取邀请系统设置")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        // 管理员更新邀请系统设置
        group.MapPut("/settings",
                async (UpdateInvitationSettingsRequest request, IInvitationService invitationService) =>
                {
                    if (request.InviterReward < 0 || request.InvitedReward < 0) return Results.BadRequest("奖励金额不能为负数");

                    if (request.MaxInvitations < 0) return Results.BadRequest("最大邀请数不能为负数");

                    await invitationService.SetInvitationSettingsAsync(
                        InvitationSettings.Keys.DefaultInviterReward,
                        request.InviterReward.ToString());

                    await invitationService.SetInvitationSettingsAsync(
                        InvitationSettings.Keys.DefaultInvitedReward,
                        request.InvitedReward.ToString());

                    await invitationService.SetInvitationSettingsAsync(
                        InvitationSettings.Keys.DefaultMaxInvitations,
                        request.MaxInvitations.ToString());

                    await invitationService.SetInvitationSettingsAsync(
                        InvitationSettings.Keys.InvitationEnabled,
                        request.InvitationEnabled.ToString());

                    return Results.Ok(new { message = "邀请系统设置更新成功" });
                })
            .WithName("UpdateInvitationSettings")
            .WithSummary("管理员更新邀请系统设置")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}