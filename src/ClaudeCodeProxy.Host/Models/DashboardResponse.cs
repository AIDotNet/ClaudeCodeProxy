namespace ClaudeCodeProxy.Host.Models;

/// <summary>
///     Dashboard数据响应模型
/// </summary>
public class DashboardResponse
{
    /// <summary>
    ///     总API Keys数量
    /// </summary>
    public int TotalApiKeys { get; set; }

    /// <summary>
    ///     活跃API Keys数量
    /// </summary>
    public int ActiveApiKeys { get; set; }

    /// <summary>
    ///     总服务账户数量
    /// </summary>
    public int TotalAccounts { get; set; }

    /// <summary>
    ///     活跃账户数量
    /// </summary>
    public int ActiveAccounts { get; set; }

    /// <summary>
    ///     限流账户数量
    /// </summary>
    public int RateLimitedAccounts { get; set; }

    /// <summary>
    ///     今日请求数
    /// </summary>
    public long TodayRequests { get; set; }

    /// <summary>
    ///     总请求数
    /// </summary>
    public long TotalRequests { get; set; }

    /// <summary>
    ///     今日输入Token数
    /// </summary>
    public long TodayInputTokens { get; set; }

    /// <summary>
    ///     今日输出Token数
    /// </summary>
    public long TodayOutputTokens { get; set; }

    /// <summary>
    ///     今日缓存创建Token数
    /// </summary>
    public long TodayCacheCreateTokens { get; set; }

    /// <summary>
    ///     今日缓存读取Token数
    /// </summary>
    public long TodayCacheReadTokens { get; set; }

    /// <summary>
    ///     总输入Token数
    /// </summary>
    public long TotalInputTokens { get; set; }

    /// <summary>
    ///     总输出Token数
    /// </summary>
    public long TotalOutputTokens { get; set; }

    /// <summary>
    ///     总缓存创建Token数
    /// </summary>
    public long TotalCacheCreateTokens { get; set; }

    /// <summary>
    ///     总缓存读取Token数
    /// </summary>
    public long TotalCacheReadTokens { get; set; }

    /// <summary>
    ///     实时RPM（每分钟请求数）
    /// </summary>
    public double RealtimeRPM { get; set; }

    /// <summary>
    ///     实时TPM（每分钟Token数）
    /// </summary>
    public double RealtimeTPM { get; set; }

    /// <summary>
    ///     指标时间窗口（分钟）
    /// </summary>
    public int MetricsWindow { get; set; } = 5;

    /// <summary>
    ///     是否为历史数据指标
    /// </summary>
    public bool IsHistoricalMetrics { get; set; }

    /// <summary>
    ///     系统状态
    /// </summary>
    public string SystemStatus { get; set; } = "正常";

    /// <summary>
    ///     系统运行时间（秒）
    /// </summary>
    public long UptimeSeconds { get; set; }
}

/// <summary>
///     费用数据响应模型
/// </summary>
public class CostDataResponse
{
    /// <summary>
    ///     今日费用信息
    /// </summary>
    public CostInfo TodayCosts { get; set; } = new();

    /// <summary>
    ///     总费用信息
    /// </summary>
    public CostInfo TotalCosts { get; set; } = new();
}

/// <summary>
///     费用信息
/// </summary>
public class CostInfo
{
    /// <summary>
    ///     原始费用数据
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    ///     格式化费用数据
    /// </summary>
    public FormattedCost Formatted { get; set; } = new();
}

/// <summary>
///     格式化费用
/// </summary>
public class FormattedCost
{
    /// <summary>
    ///     格式化的总费用
    /// </summary>
    public string TotalCost { get; set; } = "$0.00";
}

/// <summary>
///     模型统计数据
/// </summary>
public class ModelStatistics
{
    /// <summary>
    ///     模型名称
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    ///     请求数
    /// </summary>
    public long Requests { get; set; }

    /// <summary>
    ///     总Token数
    /// </summary>
    public long AllTokens { get; set; }

    /// <summary>
    ///     费用
    /// </summary>
    public decimal Cost { get; set; }

    /// <summary>
    ///     格式化费用信息
    /// </summary>
    public FormattedModelCost? Formatted { get; set; }
}

/// <summary>
///     格式化模型费用
/// </summary>
public class FormattedModelCost
{
    /// <summary>
    ///     格式化的总费用
    /// </summary>
    public string Total { get; set; } = "$0.00";
}

/// <summary>
///     趋势数据点
/// </summary>
public class TrendDataPoint
{
    /// <summary>
    ///     日期（格式：YYYY-MM-DD）
    /// </summary>
    public string? Date { get; set; }

    /// <summary>
    ///     小时（格式：YYYY-MM-DD HH:mm:ss）
    /// </summary>
    public string? Hour { get; set; }

    /// <summary>
    ///     显示标签
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    ///     输入Token数
    /// </summary>
    public long InputTokens { get; set; }

    /// <summary>
    ///     输出Token数
    /// </summary>
    public long OutputTokens { get; set; }

    /// <summary>
    ///     缓存创建Token数
    /// </summary>
    public long CacheCreateTokens { get; set; }

    /// <summary>
    ///     缓存读取Token数
    /// </summary>
    public long CacheReadTokens { get; set; }

    /// <summary>
    ///     请求数
    /// </summary>
    public long Requests { get; set; }

    /// <summary>
    ///     费用
    /// </summary>
    public decimal Cost { get; set; }
}

/// <summary>
///     API Keys趋势数据
/// </summary>
public class ApiKeysTrendResponse
{
    /// <summary>
    ///     趋势数据
    /// </summary>
    public List<ApiKeyTrendDataPoint> Data { get; set; } = new();

    /// <summary>
    ///     前10名API Keys信息
    /// </summary>
    public List<TopApiKeyInfo> TopApiKeys { get; set; } = new();

    /// <summary>
    ///     总API Key数量
    /// </summary>
    public int TotalApiKeys { get; set; }
}

/// <summary>
///     顶级API Key信息
/// </summary>
public class TopApiKeyInfo
{
    /// <summary>
    ///     API Key ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    ///     API Key名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     使用量（Token数或请求数）
    /// </summary>
    public long Usage { get; set; }

    /// <summary>
    ///     成本
    /// </summary>
    public decimal Cost { get; set; }
}

/// <summary>
///     API Key到模型的成本流向数据
/// </summary>
public class ApiKeyModelFlowData
{
    /// <summary>
    ///     API Key ID
    /// </summary>
    public string ApiKeyId { get; set; } = string.Empty;

    /// <summary>
    ///     API Key名称
    /// </summary>
    public string ApiKeyName { get; set; } = string.Empty;

    /// <summary>
    ///     模型名称
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    ///     请求数
    /// </summary>
    public long Requests { get; set; }

    /// <summary>
    ///     Token数
    /// </summary>
    public long Tokens { get; set; }

    /// <summary>
    ///     成本
    /// </summary>
    public decimal Cost { get; set; }
}

/// <summary>
///     API Key趋势数据点
/// </summary>
public class ApiKeyTrendDataPoint
{
    /// <summary>
    ///     日期（格式：YYYY-MM-DD）
    /// </summary>
    public string? Date { get; set; }

    /// <summary>
    ///     小时（格式：YYYY-MM-DD HH:mm:ss）
    /// </summary>
    public string? Hour { get; set; }

    /// <summary>
    ///     显示标签
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    ///     API Keys数据
    /// </summary>
    public Dictionary<string, ApiKeyMetric> ApiKeys { get; set; } = new();
}

/// <summary>
///     API Key指标
/// </summary>
public class ApiKeyMetric
{
    /// <summary>
    ///     API Key名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     请求数
    /// </summary>
    public long Requests { get; set; }

    /// <summary>
    ///     Token数
    /// </summary>
    public long Tokens { get; set; }

    /// <summary>
    ///     费用
    /// </summary>
    public decimal Cost { get; set; }

    /// <summary>
    ///     格式化费用
    /// </summary>
    public string FormattedCost { get; set; } = "$0.00";
}

/// <summary>
///     日期过滤器请求
/// </summary>
public class DateFilterRequest
{
    /// <summary>
    ///     过滤器类型：preset, custom
    /// </summary>
    public string Type { get; set; } = "preset";

    /// <summary>
    ///     预设值：today, yesterday, last7days, last30days
    /// </summary>
    public string? Preset { get; set; }

    /// <summary>
    ///     自定义日期范围
    /// </summary>
    public List<string>? CustomRange { get; set; }

    /// <summary>
    ///     开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    ///     结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }
}

/// <summary>
///     趋势粒度类型
/// </summary>
public enum TrendGranularity
{
    /// <summary>
    ///     按天
    /// </summary>
    Day,

    /// <summary>
    ///     按小时
    /// </summary>
    Hour
}

/// <summary>
///     API Keys趋势指标类型
/// </summary>
public enum ApiKeysTrendMetric
{
    /// <summary>
    ///     请求次数
    /// </summary>
    Requests,

    /// <summary>
    ///     Token数量
    /// </summary>
    Tokens
}

/// <summary>
///     请求日志查询请求
/// </summary>
public class RequestLogsRequest
{
    /// <summary>
    ///     页码
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    ///     每页数量
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    ///     日期过滤器
    /// </summary>
    public DateFilterRequest? DateFilter { get; set; }

    /// <summary>
    ///     API Key ID过滤
    /// </summary>
    public string? ApiKeyId { get; set; }

    /// <summary>
    ///     请求状态过滤
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    ///     模型过滤
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    ///     平台过滤
    /// </summary>
    public string? Platform { get; set; }

    /// <summary>
    ///     搜索关键词
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    ///     排序字段
    /// </summary>
    public string SortBy { get; set; } = "RequestStartTime";

    /// <summary>
    ///     排序方向
    /// </summary>
    public string SortDirection { get; set; } = "desc";
}

/// <summary>
///     请求日志响应
/// </summary>
public class RequestLogsResponse
{
    /// <summary>
    ///     日志数据列表
    /// </summary>
    public List<RequestLogSummary> Data { get; set; } = new();

    /// <summary>
    ///     总记录数
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    ///     当前页码
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    ///     每页数量
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    ///     总页数
    /// </summary>
    public int TotalPages { get; set; }
}

/// <summary>
///     请求日志摘要信息
/// </summary>
public class RequestLogSummary
{
    /// <summary>
    ///     日志ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     API Key ID
    /// </summary>
    public Guid ApiKeyId { get; set; }

    /// <summary>
    ///     API Key 名称
    /// </summary>
    public string ApiKeyName { get; set; } = string.Empty;

    /// <summary>
    ///     关联的账户ID
    /// </summary>
    public string? AccountId { get; set; }

    /// <summary>
    ///     账户名称
    /// </summary>
    public string? AccountName { get; set; }

    /// <summary>
    ///     使用的模型名称
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    ///     平台类型
    /// </summary>
    public string Platform { get; set; } = "claude";

    /// <summary>
    ///     请求开始时间
    /// </summary>
    public DateTime RequestStartTime { get; set; }

    /// <summary>
    ///     请求结束时间
    /// </summary>
    public DateTime? RequestEndTime { get; set; }

    /// <summary>
    ///     请求持续时间（毫秒）
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    ///     请求状态
    /// </summary>
    public string Status { get; set; } = "success";

    /// <summary>
    ///     错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    ///     HTTP状态码
    /// </summary>
    public int? HttpStatusCode { get; set; }

    /// <summary>
    ///     输入Token数量
    /// </summary>
    public int InputTokens { get; set; }

    /// <summary>
    ///     输出Token数量
    /// </summary>
    public int OutputTokens { get; set; }

    /// <summary>
    ///     总Token数量
    /// </summary>
    public int TotalTokens { get; set; }

    /// <summary>
    ///     请求费用
    /// </summary>
    public decimal Cost { get; set; }

    /// <summary>
    ///     是否流式响应
    /// </summary>
    public bool IsStreaming { get; set; }

    /// <summary>
    ///     客户端IP地址
    /// </summary>
    public string? ClientIp { get; set; }

    /// <summary>
    ///     请求ID
    /// </summary>
    public string? RequestId { get; set; }
}

/// <summary>
///     请求状态统计
/// </summary>
public class RequestStatusStat
{
    /// <summary>
    ///     状态名称
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    ///     请求数量
    /// </summary>
    public long Count { get; set; }

    /// <summary>
    ///     总Token数量
    /// </summary>
    public long TotalTokens { get; set; }

    /// <summary>
    ///     总费用
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    ///     平均响应时间（毫秒）
    /// </summary>
    public double AverageDurationMs { get; set; }
}

/// <summary>
///     实时请求监控响应
/// </summary>
public class RealtimeRequestsResponse
{
    /// <summary>
    ///     最近的请求列表
    /// </summary>
    public List<RealtimeRequestSummary> RecentRequests { get; set; } = new();

    /// <summary>
    ///     时间窗口（分钟）
    /// </summary>
    public int WindowMinutes { get; set; }

    /// <summary>
    ///     统计数据
    /// </summary>
    public RealtimeStats Stats { get; set; } = new();
}

/// <summary>
///     实时请求摘要
/// </summary>
public class RealtimeRequestSummary
{
    /// <summary>
    ///     日志ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     API Key 名称
    /// </summary>
    public string ApiKeyName { get; set; } = string.Empty;

    /// <summary>
    ///     模型名称
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    ///     平台类型
    /// </summary>
    public string Platform { get; set; } = "claude";

    /// <summary>
    ///     请求开始时间
    /// </summary>
    public DateTime RequestStartTime { get; set; }

    /// <summary>
    ///     请求状态
    /// </summary>
    public string Status { get; set; } = "success";

    /// <summary>
    ///     请求持续时间（毫秒）
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    ///     总Token数量
    /// </summary>
    public int TotalTokens { get; set; }

    /// <summary>
    ///     请求费用
    /// </summary>
    public decimal Cost { get; set; }

    /// <summary>
    ///     错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
///     实时统计数据
/// </summary>
public class RealtimeStats
{
    /// <summary>
    ///     总请求数
    /// </summary>
    public int TotalRequests { get; set; }

    /// <summary>
    ///     成功请求数
    /// </summary>
    public int SuccessRequests { get; set; }

    /// <summary>
    ///     成功率（百分比）
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    ///     总Token数量
    /// </summary>
    public long TotalTokens { get; set; }

    /// <summary>
    ///     平均响应时间（毫秒）
    /// </summary>
    public double AverageResponseTimeMs { get; set; }

    /// <summary>
    ///     每分钟请求数
    /// </summary>
    public double RequestsPerMinute { get; set; }
}