using System.Diagnostics;
using ClaudeCodeProxy.Core;
using ClaudeCodeProxy.Domain;
using ClaudeCodeProxy.Host.Models;
using ClaudeCodeProxy.Host.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClaudeCodeProxy.Host.Endpoints;

/// <summary>
///     Dashboard统计相关端点
/// </summary>
public static class DashboardEndpoints
{
    /// <summary>
    ///     配置Dashboard相关端点
    /// </summary>
    public static void MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboard")
            .WithTags("Dashboard")
            .WithOpenApi();

        // 获取Dashboard主要统计数据
        group.MapGet("/", GetDashboardData)
            .WithName("GetDashboardData")
            .WithSummary("获取Dashboard统计数据")
            .WithDescription("获取API Keys、账户、请求、Token等主要统计数据")
            .Produces<DashboardResponse>();

        // 获取费用数据
        group.MapGet("/costs", GetCostData)
            .WithName("GetCostData")
            .WithSummary("获取费用统计数据")
            .WithDescription("获取今日费用和总费用统计")
            .Produces<CostDataResponse>();

        // 获取模型统计数据
        group.MapPost("/model-statistics", GetModelStatistics)
            .WithName("GetModelStatistics")
            .WithSummary("获取模型使用统计")
            .WithDescription("获取各模型的使用情况统计")
            .Produces<List<ModelStatistics>>();

        // 获取趋势数据
        group.MapPost("/trend-data", GetTrendData)
            .WithName("GetTrendData")
            .WithSummary("获取使用趋势数据")
            .WithDescription("获取Token使用趋势数据，支持按天或按小时统计")
            .Produces<List<TrendDataPoint>>();

        // 获取API Keys趋势数据
        group.MapPost("/apikeys-trend", GetApiKeysTrend)
            .WithName("GetApiKeysTrend")
            .WithSummary("获取API Keys趋势数据")
            .WithDescription("获取前10名API Keys的使用趋势数据")
            .Produces<ApiKeysTrendResponse>();

        // 系统运行时间
        group.MapGet("/uptime", GetSystemUptime)
            .WithName("GetSystemUptime")
            .WithSummary("获取系统运行时间")
            .WithDescription("获取格式化的系统运行时间")
            .Produces<object>();

        // 获取请求日志列表
        group.MapPost("/request-logs", GetRequestLogs)
            .WithName("GetRequestLogs")
            .WithSummary("获取请求日志列表")
            .WithDescription("分页获取请求日志记录，支持多种过滤条件")
            .Produces<RequestLogsResponse>();

        // 获取请求日志详情
        group.MapGet("/request-logs/{id}", GetRequestLogDetail)
            .WithName("GetRequestLogDetail")
            .WithSummary("获取请求日志详情")
            .WithDescription("根据ID获取单个请求日志的详细信息")
            .Produces<RequestLog>();

        // 获取请求状态统计
        group.MapPost("/request-status-stats", GetRequestStatusStats)
            .WithName("GetRequestStatusStats")
            .WithSummary("获取请求状态统计")
            .WithDescription("获取成功、失败、超时等状态的请求统计")
            .Produces<List<RequestStatusStat>>();

        // 获取实时请求监控数据
        group.MapGet("/realtime-requests", GetRealtimeRequests)
            .WithName("GetRealtimeRequests")
            .WithSummary("获取实时请求监控")
            .WithDescription("获取最近的请求活动数据用于实时监控")
            .Produces<RealtimeRequestsResponse>();

        // 获取API Key到模型的成本流向数据
        group.MapPost("/apikey-model-flow", GetApiKeyModelFlowData)
            .WithName("GetApiKeyModelFlowData")
            .WithSummary("获取API Key到模型的成本流向数据")
            .WithDescription("获取API Key和模型之间的实际使用和成本分布数据")
            .Produces<List<ApiKeyModelFlowData>>();
    }

    /// <summary>
    ///     获取Dashboard统计数据
    /// </summary>
    private static async Task<Results<Ok<DashboardResponse>, BadRequest<string>>> GetDashboardData(
        [FromServices] StatisticsService statisticsService,
        [FromServices] IUserContext userContext,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dashboardData = await statisticsService.GetDashboardDataAsync(userContext.IsAdmin(),
                userContext.GetCurrentUserId().Value, cancellationToken);
            return TypedResults.Ok(dashboardData);
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest($"获取Dashboard数据失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     获取费用数据
    /// </summary>
    private static async Task<Results<Ok<CostDataResponse>, BadRequest<string>>> GetCostData(
        [FromServices] StatisticsService statisticsService,
        [FromServices] IUserContext userContext,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var costData =
                await statisticsService.GetCostDataAsync(userContext.GetCurrentUserId().Value, cancellationToken);
            return TypedResults.Ok(costData);
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest($"获取费用数据失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     获取模型统计数据
    /// </summary>
    private static async Task<Results<Ok<List<ModelStatistics>>, BadRequest<string>>> GetModelStatistics(
        [FromServices] StatisticsService statisticsService,
        [FromBody] DateFilterRequest? dateFilter = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            DateTime? startDate = null;
            DateTime? endDate = null;

            if (dateFilter != null)
            {
                var (start, end) = StatisticsService.ParseDateFilter(dateFilter);
                startDate = start;
                endDate = end;
            }

            var modelStats = await statisticsService.GetModelStatisticsAsync(startDate, endDate, cancellationToken);
            return TypedResults.Ok(modelStats);
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest($"获取模型统计数据失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     获取趋势数据
    /// </summary>
    private static async Task<Results<Ok<List<TrendDataPoint>>, BadRequest<string>>> GetTrendData(
        [FromServices] StatisticsService statisticsService,
        [FromBody] TrendDataRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            DateTime? startDate = null;
            DateTime? endDate = null;

            if (request.DateFilter != null)
            {
                var (start, end) = StatisticsService.ParseDateFilter(request.DateFilter);
                startDate = start;
                endDate = end;
            }

            var trendData = await statisticsService.GetTrendDataAsync(
                request.Granularity,
                startDate,
                endDate,
                cancellationToken);

            return TypedResults.Ok(trendData);
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest($"获取趋势数据失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     获取API Keys趋势数据
    /// </summary>
    private static async Task<Results<Ok<ApiKeysTrendResponse>, BadRequest<string>>> GetApiKeysTrend(
        [FromServices] StatisticsService statisticsService,
        [FromBody] ApiKeysTrendRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            DateTime? startDate = null;
            DateTime? endDate = null;

            if (request.DateFilter != null)
            {
                var (start, end) = StatisticsService.ParseDateFilter(request.DateFilter);
                startDate = start;
                endDate = end;
            }

            var trendData = await statisticsService.GetApiKeysTrendAsync(
                request.Metric,
                request.Granularity,
                startDate,
                endDate,
                cancellationToken);

            return TypedResults.Ok(trendData);
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest($"获取API Keys趋势数据失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     获取系统运行时间
    /// </summary>
    private static Ok<UptimeResponse> GetSystemUptime()
    {
        var uptime = DateTime.Now - Process.GetCurrentProcess().StartTime.ToUniversalTime();

        var days = (int)uptime.TotalDays;
        var hours = uptime.Hours;
        var minutes = uptime.Minutes;
        var seconds = uptime.Seconds;

        var uptimeText = "";
        if (days > 0) uptimeText += $"{days}天 ";
        if (hours > 0) uptimeText += $"{hours}小时 ";
        if (minutes > 0) uptimeText += $"{minutes}分钟 ";
        uptimeText += $"{seconds}秒";

        return TypedResults.Ok(new UptimeResponse
        {
            UptimeSeconds = (long)uptime.TotalSeconds,
            UptimeText = uptimeText.Trim(),
            StartTime = Process.GetCurrentProcess().StartTime.ToUniversalTime()
        });
    }

    /// <summary>
    ///     获取请求日志列表
    /// </summary>
    private static async Task<Results<Ok<RequestLogsResponse>, BadRequest<string>>> GetRequestLogs(
        [FromServices] StatisticsService statisticsService,
        [FromServices] IContext context,
        [FromServices] IUserContext userContext,
        [FromBody] RequestLogsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = context.RequestLogs
                .Where(x => x.UserId == userContext.GetCurrentUserId())
                .AsQueryable();

            // 应用过滤条件
            if (request.DateFilter != null)
            {
                var (startDate, endDate) = StatisticsService.ParseDateFilter(request.DateFilter);
                query = query.Where(x => x.RequestStartTime >= startDate && x.RequestStartTime <= endDate);
            }

            if (!string.IsNullOrEmpty(request.ApiKeyId))
                if (Guid.TryParse(request.ApiKeyId, out var apiKeyGuid))
                    query = query.Where(x => x.ApiKeyId == apiKeyGuid);

            if (!string.IsNullOrEmpty(request.Status)) query = query.Where(x => x.Status == request.Status);

            if (!string.IsNullOrEmpty(request.Model)) query = query.Where(x => x.Model.Contains(request.Model));

            if (!string.IsNullOrEmpty(request.Platform)) query = query.Where(x => x.Platform == request.Platform);

            if (!string.IsNullOrEmpty(request.SearchTerm))
                query = query.Where(x =>
                    x.ApiKeyName.Contains(request.SearchTerm) ||
                    x.AccountName!.Contains(request.SearchTerm) ||
                    x.RequestId!.Contains(request.SearchTerm) ||
                    x.ErrorMessage!.Contains(request.SearchTerm));

            // 计算总数
            var total = await query.CountAsync(cancellationToken);

            // 应用排序
            query = request.SortBy?.ToLower() switch
            {
                "requeststarttime" => request.SortDirection == "desc"
                    ? query.OrderByDescending(x => x.RequestStartTime)
                    : query.OrderBy(x => x.RequestStartTime),
                "durationms" => request.SortDirection == "desc"
                    ? query.OrderByDescending(x => x.DurationMs)
                    : query.OrderBy(x => x.DurationMs),
                "totaltokens" => request.SortDirection == "desc"
                    ? query.OrderByDescending(x => x.TotalTokens)
                    : query.OrderBy(x => x.TotalTokens),
                "cost" => request.SortDirection == "desc"
                    ? query.OrderByDescending(x => x.Cost)
                    : query.OrderBy(x => x.Cost),
                _ => query.OrderByDescending(x => x.RequestStartTime)
            };

            // 应用分页
            var logs = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => new RequestLogSummary
                {
                    Id = x.Id,
                    ApiKeyId = x.ApiKeyId,
                    ApiKeyName = x.ApiKeyName,
                    AccountId = x.AccountId,
                    AccountName = x.AccountName,
                    Model = x.Model,
                    Platform = x.Platform,
                    RequestStartTime = x.RequestStartTime,
                    RequestEndTime = x.RequestEndTime,
                    DurationMs = x.DurationMs,
                    Status = x.Status,
                    ErrorMessage = x.ErrorMessage,
                    HttpStatusCode = x.HttpStatusCode,
                    InputTokens = x.InputTokens,
                    OutputTokens = x.OutputTokens,
                    TotalTokens = x.TotalTokens,
                    Cost = x.Cost,
                    IsStreaming = x.IsStreaming,
                    ClientIp = x.ClientIp,
                    RequestId = x.RequestId
                })
                .ToListAsync(cancellationToken);

            return TypedResults.Ok(new RequestLogsResponse
            {
                Data = logs,
                Total = total,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling((double)total / request.PageSize)
            });
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest($"获取请求日志失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     获取请求日志详情
    /// </summary>
    private static async Task<Results<Ok<RequestLog>, NotFound<string>, BadRequest<string>>> GetRequestLogDetail(
        [FromServices] IContext context,
        Guid id,
        [FromServices] IUserContext userContext,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var log = await context.RequestLogs
                .Where(x => x.UserId == userContext.GetCurrentUserId())
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (log == null) return TypedResults.NotFound($"未找到ID为 {id} 的请求日志");

            return TypedResults.Ok(log);
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest($"获取请求日志详情失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     获取请求状态统计
    /// </summary>
    private static async Task<Results<Ok<List<RequestStatusStat>>, BadRequest<string>>> GetRequestStatusStats(
        [FromServices] IContext context,
        [FromServices] IUserContext userContext,
        [FromBody] DateFilterRequest? dateFilter = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = context.RequestLogs
                .Where(x => x.UserId == userContext.GetCurrentUserId())
                .AsQueryable();

            if (dateFilter != null)
            {
                var (startDate, endDate) = StatisticsService.ParseDateFilter(dateFilter);
                query = query.Where(x => x.RequestStartTime >= startDate && x.RequestStartTime <= endDate);
            }

            var stats = await query
                .GroupBy(x => x.Status)
                .Select(g => new RequestStatusStat
                {
                    Status = g.Key,
                    Count = g.LongCount(),
                    TotalTokens = g.Sum(x => (long)x.TotalTokens),
                    TotalCost = g.Sum(x => x.Cost),
                    AverageDurationMs = g.Where(x => x.DurationMs.HasValue).Average(x => (double?)x.DurationMs) ?? 0
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync(cancellationToken);

            return TypedResults.Ok(stats);
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest($"获取请求状态统计失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     获取实时请求监控数据
    /// </summary>
    private static async Task<Results<Ok<RealtimeRequestsResponse>, BadRequest<string>>> GetRealtimeRequests(
        [FromServices] IContext context,
        [FromServices] IUserContext userContext,
        int minutes = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffTime = DateTime.Now.AddMinutes(-minutes);

            var recentRequests = await context.RequestLogs
                .Where(x =>
                    x.UserId == userContext.GetCurrentUserId() &&
                    x.RequestStartTime >= cutoffTime)
                .OrderByDescending(x => x.RequestStartTime)
                .Take(50)
                .Select(x => new RealtimeRequestSummary
                {
                    Id = x.Id,
                    ApiKeyName = x.ApiKeyName,
                    Model = x.Model,
                    Platform = x.Platform,
                    RequestStartTime = x.RequestStartTime,
                    Status = x.Status,
                    DurationMs = x.DurationMs,
                    TotalTokens = x.TotalTokens,
                    Cost = x.Cost,
                    ErrorMessage = x.ErrorMessage
                })
                .ToListAsync(cancellationToken);

            // 计算统计数据
            var totalRequests = await context.RequestLogs
                .Where(x => x.UserId == userContext.GetCurrentUserId())
                .Where(x => x.RequestStartTime >= cutoffTime)
                .CountAsync(cancellationToken);

            var successRequests = await context.RequestLogs
                .Where(x => x.UserId == userContext.GetCurrentUserId())
                .Where(x => x.RequestStartTime >= cutoffTime && x.Status == "success")
                .CountAsync(cancellationToken);

            var totalTokens = await context.RequestLogs
                .Where(x => x.UserId == userContext.GetCurrentUserId())
                .Where(x => x.RequestStartTime >= cutoffTime)
                .SumAsync(x => (long)x.TotalTokens, cancellationToken);

            var avgResponseTime = await context.RequestLogs
                .Where(x => x.UserId == userContext.GetCurrentUserId())
                .Where(x => x.RequestStartTime >= cutoffTime && x.DurationMs.HasValue)
                .AverageAsync(x => (double?)x.DurationMs, cancellationToken) ?? 0;

            return TypedResults.Ok(new RealtimeRequestsResponse
            {
                RecentRequests = recentRequests,
                WindowMinutes = minutes,
                Stats = new RealtimeStats
                {
                    TotalRequests = totalRequests,
                    SuccessRequests = successRequests,
                    SuccessRate = totalRequests > 0 ? (double)successRequests / totalRequests * 100 : 0,
                    TotalTokens = totalTokens,
                    AverageResponseTimeMs = avgResponseTime,
                    RequestsPerMinute = totalRequests / (double)minutes
                }
            });
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest($"获取实时请求监控数据失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     获取API Key到模型的成本流向数据
    /// </summary>
    private static async Task<Results<Ok<List<ApiKeyModelFlowData>>, BadRequest<string>>> GetApiKeyModelFlowData(
        [FromServices] StatisticsService statisticsService,
        [FromBody] DateFilterRequest? request,
        [FromServices] IUserContext userContext,
        CancellationToken cancellationToken = default)
    {
        try
        {
            DateTime? startDate = null;
            DateTime? endDate = null;

            if (request != null)
            {
                var (parsedStartDate, parsedEndDate) = StatisticsService.ParseDateFilter(request);
                startDate = parsedStartDate;
                endDate = parsedEndDate;
            }

            var flowData = await statisticsService.GetApiKeyModelFlowDataAsync(userContext.GetCurrentUserId()!.Value,
                startDate, endDate, cancellationToken);
            return TypedResults.Ok(flowData);
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest($"获取成本流向数据失败: {ex.Message}");
        }
    }
}

/// <summary>
///     趋势数据请求
/// </summary>
public class TrendDataRequest
{
    /// <summary>
    ///     趋势粒度
    /// </summary>
    public TrendGranularity Granularity { get; set; } = TrendGranularity.Day;

    /// <summary>
    ///     日期过滤器
    /// </summary>
    public DateFilterRequest? DateFilter { get; set; }
}

/// <summary>
///     API Keys趋势请求
/// </summary>
public class ApiKeysTrendRequest
{
    /// <summary>
    ///     指标类型
    /// </summary>
    public ApiKeysTrendMetric Metric { get; set; } = ApiKeysTrendMetric.Tokens;

    /// <summary>
    ///     趋势粒度
    /// </summary>
    public TrendGranularity Granularity { get; set; } = TrendGranularity.Day;

    /// <summary>
    ///     日期过滤器
    /// </summary>
    public DateFilterRequest? DateFilter { get; set; }
}

/// <summary>
///     系统运行时间响应
/// </summary>
public class UptimeResponse
{
    /// <summary>
    ///     运行时间（秒）
    /// </summary>
    public long UptimeSeconds { get; set; }

    /// <summary>
    ///     格式化的运行时间文本
    /// </summary>
    public string UptimeText { get; set; } = string.Empty;

    /// <summary>
    ///     系统启动时间
    /// </summary>
    public DateTime StartTime { get; set; }
}