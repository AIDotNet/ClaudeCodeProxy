namespace ClaudeCodeProxy.Domain;

/// <summary>
///     限流异常
/// </summary>
public class RateLimitException : Exception
{
    public RateLimitException(string message, RateLimitInfo rateLimitInfo) : base(message)
    {
        RateLimitInfo = rateLimitInfo;
    }

    public RateLimitException(string message, RateLimitInfo rateLimitInfo, Exception innerException)
        : base(message, innerException)
    {
        RateLimitInfo = rateLimitInfo;
    }

    /// <summary>
    ///     限流信息
    /// </summary>
    public RateLimitInfo RateLimitInfo { get; }
}