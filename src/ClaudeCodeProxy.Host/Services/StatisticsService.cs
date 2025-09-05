using System.Globalization;
using ClaudeCodeProxy.Core;
using ClaudeCodeProxy.Domain;
using ClaudeCodeProxy.Host.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaudeCodeProxy.Host.Services;

/// <summary>
///     统计服务类
///     处理请求日志记录、实时指标计算和统计数据查询
/// </summary>
public class StatisticsService
{
    private static readonly DateTime _systemStartTime = DateTime.Now;
    private readonly IContext _context;
    private readonly ILogger<StatisticsService> _logger;

    public StatisticsService(IContext context, ILogger<StatisticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    ///     记录请求日志
    /// </summary>
    public async Task<RequestLog> LogRequestAsync(
        Guid apiKeyId,
        string apiKeyName,
        string? accountId,
        string? accountName,
        string model,
        string platform = "claude",
        bool isStreaming = false,
        string? requestId = null,
        string? clientIp = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        var requestLog = new RequestLog
        {
            Id = Guid.NewGuid(),
            ApiKeyId = apiKeyId,
            ApiKeyName = apiKeyName,
            AccountId = accountId,
            AccountName = accountName,
            Model = model,
            Platform = platform,
            IsStreaming = isStreaming,
            RequestId = requestId,
            ClientIp = clientIp,
            UserAgent = userAgent,
            RequestStartTime = DateTime.Now,
            CreatedAt = DateTime.Now
        };

        requestLog.InitializeTimeFields();

        _context.RequestLogs.Add(requestLog);
        await _context.SaveAsync(cancellationToken);

        return requestLog;
    }

    /// <summary>
    ///     完成请求日志记录
    /// </summary>
    public async Task CompleteRequestLogAsync(
        Guid requestLogId,
        int inputTokens = 0,
        int outputTokens = 0,
        int cacheCreateTokens = 0,
        int cacheReadTokens = 0,
        decimal cost = 0,
        string status = "success",
        string? errorMessage = null,
        int? httpStatusCode = null,
        CancellationToken cancellationToken = default)
    {
        var requestLog = await _context.RequestLogs
            .FirstOrDefaultAsync(x => x.Id == requestLogId, cancellationToken);

        if (requestLog == null)
        {
            _logger.LogWarning("Request log with ID {RequestLogId} not found", requestLogId);
            return;
        }

        requestLog.InputTokens = inputTokens;
        requestLog.OutputTokens = outputTokens;
        requestLog.CacheCreateTokens = cacheCreateTokens;
        requestLog.CacheReadTokens = cacheReadTokens;
        requestLog.Cost = cost;
        requestLog.HttpStatusCode = httpStatusCode;
        requestLog.CalculateTotalTokens();
        requestLog.CompleteRequest(DateTime.Now, status, errorMessage);

        // 同时更新 API Key 统计信息
        // 无论成功还是失败都要增加使用次数，但只有成功才累加费用
        await UpdateApiKeyStatisticsAsync(requestLog.ApiKeyId, status == "success" ? cost : 0, cancellationToken);

        await _context.SaveAsync(cancellationToken);
    }

    /// <summary>
    ///     更新 API Key 统计信息
    /// </summary>
    public async Task UpdateApiKeyStatisticsAsync(
        Guid apiKeyId,
        decimal cost = 0,
        CancellationToken cancellationToken = default)
    {
        var apiKey = await _context.ApiKeys
            .FirstOrDefaultAsync(x => x.Id == apiKeyId, cancellationToken);

        if (apiKey == null)
        {
            _logger.LogWarning("API Key with ID {ApiKeyId} not found", apiKeyId);
            return;
        }

        var now = DateTime.Now;
        var today = now.Date;
        var currentMonth = new DateTime(now.Year, now.Month, 1);

        // 检查是否需要重置每日使用量
        var lastUsedDate = apiKey.LastUsedAt?.Date;
        if (lastUsedDate.HasValue && lastUsedDate.Value < today) apiKey.DailyCostUsed = 0;

        // 检查是否需要重置月度使用量
        var lastUsedMonth = apiKey.LastUsedAt.HasValue
            ? new DateTime(apiKey.LastUsedAt.Value.Year, apiKey.LastUsedAt.Value.Month, 1)
            : DateTime.MinValue;
        if (lastUsedMonth < currentMonth) apiKey.MonthlyCostUsed = 0;

        // 增加使用次数
        apiKey.TotalUsageCount++;

        // 累加各种费用
        apiKey.TotalCost += cost;
        apiKey.DailyCostUsed += cost;
        apiKey.MonthlyCostUsed += cost;

        // 更新最后使用时间
        apiKey.LastUsedAt = now;

        // 注意：这里不调用 SaveAsync，因为会在 CompleteRequestLogAsync 中统一保存
    }

    /// <summary>
    ///     获取Dashboard统计数据
    /// </summary>
    public async Task<DashboardResponse> GetDashboardDataAsync(
        bool admin,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.Now.Date;
        var now = DateTime.Now;

        // 顺序查询，避免EF Core并发问题
        var totalApiKeys = await _context.ApiKeys
            .Where(x => x.UserId == currentUserId)
            .CountAsync(cancellationToken);
        var activeApiKeys = await _context.ApiKeys
            .Where(x => x.UserId == currentUserId)
            .CountAsync(x => x.IsEnabled && (x.ExpiresAt == null || x.ExpiresAt > DateTime.Now), cancellationToken);

        var totalAccounts = 0;
        var activeAccounts = 0;
        var rateLimitedAccounts = 0;
        if (admin)
        {
            totalAccounts = await _context.Accounts
                .CountAsync(cancellationToken);
            activeAccounts = await _context.Accounts
                .CountAsync(x => x.IsEnabled && x.Status == "active", cancellationToken);
            rateLimitedAccounts = await _context.Accounts
                .CountAsync(x => x.Status == "rate_limited", cancellationToken);
        }

        var todayRequests = await _context.RequestLogs
            .Where(x => x.UserId == currentUserId)
            .Where(x => x.RequestDate == today)
            .LongCountAsync(cancellationToken);
        var totalRequests = await _context.RequestLogs
            .Where(x => x.UserId == currentUserId)
            .LongCountAsync(cancellationToken);
        var todayInputTokens = await _context.RequestLogs
            .Where(x => x.UserId == currentUserId)
            .Where(x => x.RequestDate == today)
            .SumAsync(x => (long)x.InputTokens, cancellationToken);
        var todayOutputTokens = await _context.RequestLogs
            .Where(x => x.UserId == currentUserId)
            .Where(x => x.RequestDate == today)
            .SumAsync(x => (long)x.OutputTokens, cancellationToken);
        var todayCacheCreateTokens = await _context.RequestLogs
            .Where(x => x.UserId == currentUserId)
            .Where(x => x.RequestDate == today)
            .SumAsync(x => (long)x.CacheCreateTokens, cancellationToken);
        var todayCacheReadTokens = await _context.RequestLogs
            .Where(x => x.UserId == currentUserId)
            .Where(x => x.RequestDate == today)
            .SumAsync(x => (long)x.CacheReadTokens, cancellationToken);
        var totalInputTokens = await _context.RequestLogs
            .Where(x => x.UserId == currentUserId)
            .SumAsync(x => (long)x.InputTokens, cancellationToken);
        var totalOutputTokens = await _context.RequestLogs
            .Where(x => x.UserId == currentUserId)
            .SumAsync(x => (long)x.OutputTokens, cancellationToken);
        var totalCacheCreateTokens = await _context.RequestLogs
            .Where(x => x.UserId == currentUserId)
            .SumAsync(x => (long)x.CacheCreateTokens, cancellationToken);
        var totalCacheReadTokens = await _context.RequestLogs
            .Where(x => x.UserId == currentUserId)
            .SumAsync(x => (long)x.CacheReadTokens, cancellationToken);

        // 计算实时RPM和TPM
        var (rpm, tpm, isHistorical) = await CalculateRealtimeMetricsAsync(currentUserId, 5, cancellationToken);

        return new DashboardResponse
        {
            TotalApiKeys = totalApiKeys,
            ActiveApiKeys = activeApiKeys,
            TotalAccounts = totalAccounts,
            ActiveAccounts = activeAccounts,
            RateLimitedAccounts = rateLimitedAccounts,
            TodayRequests = todayRequests,
            TotalRequests = totalRequests,
            TodayInputTokens = todayInputTokens,
            TodayOutputTokens = todayOutputTokens,
            TodayCacheCreateTokens = todayCacheCreateTokens,
            TodayCacheReadTokens = todayCacheReadTokens,
            TotalInputTokens = totalInputTokens,
            TotalOutputTokens = totalOutputTokens,
            TotalCacheCreateTokens = totalCacheCreateTokens,
            TotalCacheReadTokens = totalCacheReadTokens,
            RealtimeRPM = rpm,
            RealtimeTPM = tpm,
            MetricsWindow = 5,
            IsHistoricalMetrics = isHistorical,
            SystemStatus = "正常",
            UptimeSeconds = (long)(now - _systemStartTime).TotalSeconds
        };
    }

    /// <summary>
    ///     获取费用数据
    /// </summary>
    public async Task<CostDataResponse> GetCostDataAsync(Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.Now.Date;

        var todayCost = await _context.RequestLogs
            .Where(x => x.UserId == currentUserId)
            .Where(x => x.RequestDate == today)
            .SumAsync(x => x.Cost, cancellationToken);

        var totalCost = await _context.RequestLogs
            .Where(x => x.UserId == currentUserId)
            .SumAsync(x => x.Cost, cancellationToken);

        return new CostDataResponse
        {
            TodayCosts = new CostInfo
            {
                TotalCost = todayCost,
                Formatted = new FormattedCost
                {
                    TotalCost = FormatCost(todayCost)
                }
            },
            TotalCosts = new CostInfo
            {
                TotalCost = totalCost,
                Formatted = new FormattedCost
                {
                    TotalCost = FormatCost(totalCost)
                }
            }
        };
    }

    /// <summary>
    ///     获取模型统计数据
    /// </summary>
    public async Task<List<ModelStatistics>> GetModelStatisticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.RequestLogs.AsQueryable();

        if (startDate.HasValue)
            query = query.Where(x => x.RequestDate >= startDate.Value.Date);

        if (endDate.HasValue)
            query = query.Where(x => x.RequestDate <= endDate.Value.Date);

        var modelStats = await query
            .GroupBy(x => x.Model)
            .Select(g => new ModelStatistics
            {
                Model = g.Key,
                Requests = g.LongCount(),
                AllTokens = g.Sum(x => (long)x.TotalTokens),
                Cost = g.Sum(x => x.Cost)
            })
            .OrderByDescending(x => x.AllTokens)
            .ToListAsync(cancellationToken);

        // 添加格式化费用信息
        foreach (var stat in modelStats)
            stat.Formatted = new FormattedModelCost
            {
                Total = FormatCost(stat.Cost)
            };

        return modelStats;
    }

    /// <summary>
    ///     获取趋势数据
    /// </summary>
    public async Task<List<TrendDataPoint>> GetTrendDataAsync(
        TrendGranularity granularity,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var defaultStartDate = DateTime.Now.AddDays(-7).Date;
        var defaultEndDate = DateTime.Now.Date;

        startDate ??= defaultStartDate;
        endDate ??= defaultEndDate;

        if (granularity == TrendGranularity.Hour)
            return await GetHourlyTrendDataAsync(startDate.Value, endDate.Value, cancellationToken);

        return await GetDailyTrendDataAsync(startDate.Value, endDate.Value, cancellationToken);
    }

    /// <summary>
    ///     获取API Keys趋势数据
    /// </summary>
    public async Task<ApiKeysTrendResponse> GetApiKeysTrendAsync(
        ApiKeysTrendMetric metric,
        TrendGranularity granularity,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var defaultStartDate = DateTime.Now.AddDays(-7).Date;
        var defaultEndDate = DateTime.Now.Date;

        startDate ??= defaultStartDate;
        endDate ??= defaultEndDate;

        // 获取使用量前10的API Keys
        var topApiKeysData = await _context.RequestLogs
            .Where(x => x.RequestDate >= startDate && x.RequestDate <= endDate)
            .GroupBy(x => new { x.ApiKeyId, x.ApiKeyName })
            .Select(g => new
            {
                g.Key.ApiKeyId,
                g.Key.ApiKeyName,
                TotalTokens = g.Sum(x => (long)x.TotalTokens),
                TotalCost = g.Sum(x => x.Cost)
            })
            .OrderByDescending(x => x.TotalTokens)
            .Take(10)
            .ToListAsync(cancellationToken);

        var topApiKeys = topApiKeysData.Select(x => new TopApiKeyInfo
        {
            Id = x.ApiKeyId.ToString(),
            Name = x.ApiKeyName ?? $"Key-{x.ApiKeyId.ToString().Substring(0, 8)}",
            Usage = x.TotalTokens,
            Cost = x.TotalCost
        }).ToList();

        // 为了兼容现有代码，保留字符串ID列表
        var topApiKeyIds = topApiKeysData.Select(x => x.ApiKeyId.ToString()).ToList();

        var trendData = new List<ApiKeyTrendDataPoint>();

        if (granularity == TrendGranularity.Hour)
            trendData = await GetApiKeysHourlyTrendDataAsync(topApiKeyIds, metric, startDate.Value, endDate.Value,
                cancellationToken);
        else
            trendData = await GetApiKeysDailyTrendDataAsync(topApiKeyIds, metric, startDate.Value, endDate.Value,
                cancellationToken);

        return new ApiKeysTrendResponse
        {
            Data = trendData,
            TopApiKeys = topApiKeys,
            TotalApiKeys = await _context.ApiKeys.CountAsync(cancellationToken)
        };
    }

    /// <summary>
    ///     计算实时RPM和TPM
    /// </summary>
    private async Task<(double rpm, double tpm, bool isHistorical)> CalculateRealtimeMetricsAsync(Guid currentUserId,
        int windowMinutes,
        CancellationToken cancellationToken = default)
    {
        var windowStart = DateTime.Now.AddMinutes(-windowMinutes);
        var windowEnd = DateTime.Now;

        var recentRequests = await _context.RequestLogs
            .Where(x => x.UserId == currentUserId)
            .Where(x => x.RequestStartTime >= windowStart && x.RequestStartTime <= windowEnd)
            .ToListAsync(cancellationToken);

        if (!recentRequests.Any())
        {
            // 如果没有最近的请求，查看是否有历史数据
            var hasHistoricalData = await _context.RequestLogs
                .Where(x => x.UserId == currentUserId).AnyAsync(cancellationToken);
            return (0, 0, hasHistoricalData);
        }

        var actualWindowMinutes = (DateTime.Now - recentRequests.Min(x => x.RequestStartTime)).TotalMinutes;
        if (actualWindowMinutes < 1) actualWindowMinutes = 1; // 至少1分钟

        var requestCount = recentRequests.Count;
        var tokenCount = recentRequests.Sum(x => x.TotalTokens);

        var rpm = requestCount / actualWindowMinutes;
        var tpm = tokenCount / actualWindowMinutes;

        return (rpm, tpm, false);
    }

    /// <summary>
    ///     获取日趋势数据
    /// </summary>
    private async Task<List<TrendDataPoint>> GetDailyTrendDataAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        // 先获取聚合数据（不包含字符串格式化）
        var aggregateData = await _context.RequestLogs
            .Where(x => x.RequestDate >= startDate && x.RequestDate <= endDate)
            .GroupBy(x => x.RequestDate)
            .Select(g => new
            {
                RequestDate = g.Key,
                InputTokens = g.Sum(x => (long)x.InputTokens),
                OutputTokens = g.Sum(x => (long)x.OutputTokens),
                CacheCreateTokens = g.Sum(x => (long)x.CacheCreateTokens),
                CacheReadTokens = g.Sum(x => (long)x.CacheReadTokens),
                Requests = g.LongCount(),
                Cost = g.Sum(x => x.Cost)
            })
            .OrderBy(x => x.RequestDate)
            .ToListAsync(cancellationToken);

        // 在内存中进行字符串格式化
        return aggregateData.Select(item => new TrendDataPoint
        {
            Date = item.RequestDate.ToString("yyyy-MM-dd"),
            Label = item.RequestDate.ToString("MM/dd"),
            InputTokens = item.InputTokens,
            OutputTokens = item.OutputTokens,
            CacheCreateTokens = item.CacheCreateTokens,
            CacheReadTokens = item.CacheReadTokens,
            Requests = item.Requests,
            Cost = item.Cost
        }).ToList();
    }

    /// <summary>
    ///     获取小时趋势数据
    /// </summary>
    private async Task<List<TrendDataPoint>> GetHourlyTrendDataAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        // 先获取聚合数据（不包含字符串格式化）
        var aggregateData = await _context.RequestLogs
            .Where(x => x.RequestStartTime >= startDate && x.RequestStartTime <= endDate.AddDays(1))
            .GroupBy(x => new { x.RequestDate, x.RequestHour })
            .Select(g => new
            {
                g.Key.RequestDate,
                g.Key.RequestHour,
                InputTokens = g.Sum(x => (long)x.InputTokens),
                OutputTokens = g.Sum(x => (long)x.OutputTokens),
                CacheCreateTokens = g.Sum(x => (long)x.CacheCreateTokens),
                CacheReadTokens = g.Sum(x => (long)x.CacheReadTokens),
                Requests = g.LongCount(),
                Cost = g.Sum(x => x.Cost)
            })
            .OrderBy(x => x.RequestDate).ThenBy(x => x.RequestHour)
            .ToListAsync(cancellationToken);

        // 在内存中进行字符串格式化
        return aggregateData.Select(item => new TrendDataPoint
        {
            Hour = item.RequestDate.AddHours(item.RequestHour).ToString("yyyy-MM-dd HH:mm:ss"),
            Label = item.RequestDate.AddHours(item.RequestHour).ToString("MM/dd HH:00"),
            InputTokens = item.InputTokens,
            OutputTokens = item.OutputTokens,
            CacheCreateTokens = item.CacheCreateTokens,
            CacheReadTokens = item.CacheReadTokens,
            Requests = item.Requests,
            Cost = item.Cost
        }).ToList();
    }

    /// <summary>
    ///     获取API Keys日趋势数据
    /// </summary>
    private async Task<List<ApiKeyTrendDataPoint>> GetApiKeysDailyTrendDataAsync(
        List<string> topApiKeyIds,
        ApiKeysTrendMetric metric,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var topApiKeyGuids = topApiKeyIds.Select(Guid.Parse).ToList();

        var query = _context.RequestLogs
            .Where(x => x.RequestDate >= startDate && x.RequestDate <= endDate && topApiKeyGuids.Contains(x.ApiKeyId))
            .GroupBy(x => new { x.RequestDate, x.ApiKeyId, x.ApiKeyName })
            .Select(g => new
            {
                Date = g.Key.RequestDate,
                g.Key.ApiKeyId,
                g.Key.ApiKeyName,
                Requests = g.LongCount(),
                Tokens = g.Sum(x => (long)x.TotalTokens),
                Cost = g.Sum(x => x.Cost)
            });

        var groupedData = await query.ToListAsync(cancellationToken);

        var result = groupedData
            .GroupBy(x => x.Date)
            .Select(g => new ApiKeyTrendDataPoint
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                Label = g.Key.ToString("MM/dd"),
                ApiKeys = g.ToDictionary(
                    x => x.ApiKeyId.ToString(),
                    x => new ApiKeyMetric
                    {
                        Name = x.ApiKeyName,
                        Requests = x.Requests,
                        Tokens = x.Tokens,
                        Cost = x.Cost,
                        FormattedCost = FormatCost(x.Cost)
                    })
            })
            .OrderBy(x => x.Date)
            .ToList();

        return result;
    }

    /// <summary>
    ///     获取API Keys小时趋势数据
    /// </summary>
    private async Task<List<ApiKeyTrendDataPoint>> GetApiKeysHourlyTrendDataAsync(
        List<string> topApiKeyIds,
        ApiKeysTrendMetric metric,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var topApiKeyGuids = topApiKeyIds.Select(Guid.Parse).ToList();

        var query = _context.RequestLogs
            .Where(x => x.RequestStartTime >= startDate && x.RequestStartTime <= endDate.AddDays(1) &&
                        topApiKeyGuids.Contains(x.ApiKeyId))
            .GroupBy(x => new { x.RequestDate, x.RequestHour, x.ApiKeyId, x.ApiKeyName })
            .Select(g => new
            {
                DateTime = g.Key.RequestDate.AddHours(g.Key.RequestHour),
                g.Key.ApiKeyId,
                g.Key.ApiKeyName,
                Requests = g.LongCount(),
                Tokens = g.Sum(x => (long)x.TotalTokens),
                Cost = g.Sum(x => x.Cost)
            });

        var groupedData = await query.ToListAsync(cancellationToken);

        var result = groupedData
            .GroupBy(x => x.DateTime)
            .Select(g => new ApiKeyTrendDataPoint
            {
                Hour = g.Key.ToString("yyyy-MM-dd HH:mm:ss"),
                Label = g.Key.ToString("MM/dd HH:00"),
                ApiKeys = g.ToDictionary(
                    x => x.ApiKeyId.ToString(),
                    x => new ApiKeyMetric
                    {
                        Name = x.ApiKeyName,
                        Requests = x.Requests,
                        Tokens = x.Tokens,
                        Cost = x.Cost,
                        FormattedCost = FormatCost(x.Cost)
                    })
            })
            .OrderBy(x => x.Hour)
            .ToList();

        return result;
    }

    /// <summary>
    ///     格式化费用显示
    /// </summary>
    private static string FormatCost(decimal cost)
    {
        if (cost == 0) return "$0.00";
        if (cost < 0.01m) return cost.ToString("$0.000000", CultureInfo.InvariantCulture);
        return cost.ToString("$0.0000", CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     解析日期过滤器
    /// </summary>
    public static (DateTime startDate, DateTime endDate) ParseDateFilter(DateFilterRequest filter)
    {
        var now = DateTime.Now;
        var today = now.Date;

        if (filter.Type == "custom" && filter.StartTime.HasValue && filter.EndTime.HasValue)
            return (filter.StartTime.Value, filter.EndTime.Value);

        return filter.Preset switch
        {
            "today" => (today, today.AddDays(1).AddTicks(-1)),
            "yesterday" => (today.AddDays(-1), today.AddTicks(-1)),
            "last7days" => (today.AddDays(-7), today.AddDays(1).AddTicks(-1)),
            "last30days" => (today.AddDays(-30), today.AddDays(1).AddTicks(-1)),
            _ => (today.AddDays(-7), today.AddDays(1).AddTicks(-1))
        };
    }

    /// <summary>
    ///     获取API Key到模型的成本流向数据
    /// </summary>
    public async Task<List<ApiKeyModelFlowData>> GetApiKeyModelFlowDataAsync(Guid currentUserId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        startDate ??= DateTime.Now.AddDays(-7).Date;
        endDate ??= DateTime.Now.Date.AddDays(1);

        var groupedData = await _context.RequestLogs
            .Where(x => x.UserId == currentUserId)
            .Where(x => x.RequestStartTime >= startDate && x.RequestStartTime < endDate)
            .GroupBy(x => new { x.ApiKeyId, x.ApiKeyName, x.Model })
            .Select(g => new ApiKeyModelFlowData
            {
                ApiKeyId = g.Key.ApiKeyId.ToString(),
                ApiKeyName = g.Key.ApiKeyName ?? $"Key-{g.Key.ApiKeyId.ToString().Substring(0, 8)}",
                Model = g.Key.Model,
                Requests = g.LongCount(),
                Tokens = g.Sum(x => (long)(x.InputTokens + x.OutputTokens)),
                Cost = g.Sum(x => x.Cost)
            })
            .Where(x => x.Cost > 0) // 只返回有成本的流向
            .ToListAsync(cancellationToken);

        // 在内存中排序和限制数量，避免SQLite decimal排序问题
        var flowData = groupedData
            .OrderByDescending(x => x.Cost)
            .Take(50) // 限制返回最多50个流向，避免数据过多
            .ToList();

        return flowData;
    }
}