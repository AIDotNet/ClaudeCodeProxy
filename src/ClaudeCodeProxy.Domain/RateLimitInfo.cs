namespace ClaudeCodeProxy.Domain;

/// <summary>
///     限流信息
/// </summary>
public class RateLimitInfo
{
    /// <summary>
    ///     HTTP状态码
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    ///     错误消息
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    ///     需要等待的秒数
    /// </summary>
    public int RetryAfterSeconds { get; set; }

    /// <summary>
    ///     发生时间
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    ///     限流类型（从错误消息中解析）
    /// </summary>
    public string? LimitType { get; set; }

    /// <summary>
    ///     解除限流的时间
    /// </summary>
    public DateTime RateLimitedUntil => Timestamp.AddSeconds(RetryAfterSeconds);
}