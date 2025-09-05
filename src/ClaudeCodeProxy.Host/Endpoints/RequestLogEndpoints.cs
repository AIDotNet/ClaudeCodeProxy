using ClaudeCodeProxy.Core;
using ClaudeCodeProxy.Host.Services;

namespace ClaudeCodeProxy.Host.Endpoints;

/// <summary>
///     请求日志相关API端点
/// </summary>
public static class RequestLogEndpoints
{
    public static void MapRequestLogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/request-logs")
            .WithTags("RequestLogs")
            .RequireAuthorization();

        // 获取当前用户请求日志
        group.MapGet("/", GetUserRequestLogs)
            .WithName("GetUserRequestLogs")
            .WithSummary("获取当前用户请求日志")
            .WithOpenApi();

        // 获取当前用户请求统计
        group.MapGet("/statistics", GetUserRequestStatistics)
            .WithName("GetUserRequestStatistics")
            .WithSummary("获取当前用户请求统计")
            .WithOpenApi();

        // 管理员获取所有请求日志
        group.MapGet("/all", GetAllRequestLogs)
            .WithName("GetAllRequestLogs")
            .WithSummary("管理员获取所有请求日志")
            .WithOpenApi()
            .RequireAuthorization("Admin");

        // 管理员获取指定用户请求日志
        group.MapGet("/user/{userId:Guid}", GetUserRequestLogsByAdmin)
            .WithName("GetUserRequestLogsByAdmin")
            .WithSummary("管理员获取指定用户请求日志")
            .WithOpenApi()
            .RequireAuthorization("Admin");

        // 管理员获取指定用户请求统计
        group.MapGet("/user/{userId:Guid}/statistics", GetUserRequestStatisticsByAdmin)
            .WithName("GetUserRequestStatisticsByAdmin")
            .WithSummary("管理员获取指定用户请求统计")
            .WithOpenApi()
            .RequireAuthorization("Admin");
    }

    /// <summary>
    ///     获取当前用户请求日志
    /// </summary>
    private static async Task<IResult> GetUserRequestLogs(
        IUserContext userContext,
        RequestLogService requestLogService,
        int pageIndex = 0,
        int pageSize = 20,
        string? status = null,
        string? model = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var userId = userContext.GetCurrentUserId();
        if (userId == null) return Results.Unauthorized();

        try
        {
            var logs = await requestLogService.GetUserRequestLogsAsync(
                userId.Value, pageIndex, pageSize, status, model, startDate, endDate);
            return Results.Ok(logs);
        }
        catch (Exception ex)
        {
            return Results.Problem($"获取请求日志失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     获取当前用户请求统计
    /// </summary>
    private static async Task<IResult> GetUserRequestStatistics(
        IUserContext userContext,
        RequestLogService requestLogService,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var userId = userContext.GetCurrentUserId();
        if (userId == null) return Results.Unauthorized();

        try
        {
            var statistics = await requestLogService.GetUserRequestStatisticsAsync(userId.Value, startDate, endDate);
            return Results.Ok(statistics);
        }
        catch (Exception ex)
        {
            return Results.Problem($"获取请求统计失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     管理员获取所有请求日志
    /// </summary>
    private static async Task<IResult> GetAllRequestLogs(
        RequestLogService requestLogService,
        int pageIndex = 0,
        int pageSize = 20,
        string? status = null,
        string? model = null,
        Guid? userId = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            var logs = await requestLogService.GetAllRequestLogsAsync(
                pageIndex, pageSize, status, model, userId, startDate, endDate);
            return Results.Ok(logs);
        }
        catch (Exception ex)
        {
            return Results.Problem($"获取请求日志失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     管理员获取指定用户请求日志
    /// </summary>
    private static async Task<IResult> GetUserRequestLogsByAdmin(
        Guid userId,
        RequestLogService requestLogService,
        int pageIndex = 0,
        int pageSize = 20,
        string? status = null,
        string? model = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            var logs = await requestLogService.GetUserRequestLogsAsync(
                userId, pageIndex, pageSize, status, model, startDate, endDate);
            return Results.Ok(logs);
        }
        catch (Exception ex)
        {
            return Results.Problem($"获取请求日志失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     管理员获取指定用户请求统计
    /// </summary>
    private static async Task<IResult> GetUserRequestStatisticsByAdmin(
        Guid userId,
        RequestLogService requestLogService,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            var statistics = await requestLogService.GetUserRequestStatisticsAsync(userId, startDate, endDate);
            return Results.Ok(statistics);
        }
        catch (Exception ex)
        {
            return Results.Problem($"获取请求统计失败: {ex.Message}");
        }
    }
}