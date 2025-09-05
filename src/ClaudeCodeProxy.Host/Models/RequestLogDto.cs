namespace ClaudeCodeProxy.Host.Models;

/// <summary>
///     请求日志DTO
/// </summary>
public class RequestLogDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public Guid ApiKeyId { get; set; }
    public string ApiKeyName { get; set; } = string.Empty;
    public string? AccountId { get; set; }
    public string? AccountName { get; set; }
    public string Model { get; set; } = string.Empty;
    public DateTime RequestStartTime { get; set; }
    public DateTime? RequestEndTime { get; set; }
    public long? DurationMs { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int? HttpStatusCode { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int CacheCreateTokens { get; set; }
    public int CacheReadTokens { get; set; }
    public int TotalTokens { get; set; }
    public decimal Cost { get; set; }
    public string? ClientIp { get; set; }
    public string? UserAgent { get; set; }
    public string? RequestId { get; set; }
    public bool IsStreaming { get; set; }
    public string Platform { get; set; } = "claude";
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
///     用户请求统计DTO
/// </summary>
public class UserRequestStatisticsDto
{
    public Guid UserId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public double SuccessRate { get; set; }
    public long TotalTokens { get; set; }
    public decimal TotalCost { get; set; }
    public double AverageResponseTime { get; set; }
    public List<ModelUsageDto> ModelUsage { get; set; } = new();
    public List<DailyUsageDto> DailyUsage { get; set; } = new();
}

/// <summary>
///     模型使用情况DTO
/// </summary>
public class ModelUsageDto
{
    public string Model { get; set; } = string.Empty;
    public int RequestCount { get; set; }
    public long TotalTokens { get; set; }
    public decimal TotalCost { get; set; }
}

/// <summary>
///     每日使用情况DTO
/// </summary>
public class DailyUsageDto
{
    public DateTime Date { get; set; }
    public int RequestCount { get; set; }
    public long TotalTokens { get; set; }
    public decimal TotalCost { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
}