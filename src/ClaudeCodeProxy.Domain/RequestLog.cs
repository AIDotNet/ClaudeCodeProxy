namespace ClaudeCodeProxy.Domain;

/// <summary>
///     请求日志实体类
///     记录每个API请求的详细信息用于统计分析
/// </summary>
public sealed class RequestLog : Entity<Guid>
{
    /// <summary>
    ///     用户ID - 请求发起者
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    ///     API Key ID
    /// </summary>
    public Guid ApiKeyId { get; set; }

    /// <summary>
    ///     API Key 名称（冗余字段，便于查询）
    /// </summary>
    public string ApiKeyName { get; set; } = string.Empty;

    /// <summary>
    ///     关联的账户ID
    /// </summary>
    public string? AccountId { get; set; }

    /// <summary>
    ///     账户名称（冗余字段，便于查询）
    /// </summary>
    public string? AccountName { get; set; }

    /// <summary>
    ///     使用的模型名称
    /// </summary>
    public string Model { get; set; } = string.Empty;

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
    ///     请求状态：success, error, timeout
    /// </summary>
    public string Status { get; set; } = "success";

    /// <summary>
    ///     错误信息（如果有）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    ///     HTTP状态码
    /// </summary>
    public int? HttpStatusCode { get; set; }

    /// <summary>
    ///     输入Token数量
    /// </summary>
    public int InputTokens { get; set; } = 0;

    /// <summary>
    ///     输出Token数量
    /// </summary>
    public int OutputTokens { get; set; } = 0;

    /// <summary>
    ///     缓存创建Token数量
    /// </summary>
    public int CacheCreateTokens { get; set; } = 0;

    /// <summary>
    ///     缓存读取Token数量
    /// </summary>
    public int CacheReadTokens { get; set; } = 0;

    /// <summary>
    ///     总Token数量
    /// </summary>
    public int TotalTokens { get; set; }

    /// <summary>
    ///     请求费用（美元）
    /// </summary>
    public decimal Cost { get; set; } = 0;

    /// <summary>
    ///     客户端IP地址
    /// </summary>
    public string? ClientIp { get; set; }

    /// <summary>
    ///     客户端User-Agent
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    ///     请求ID（用于链路追踪）
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    ///     是否流式响应
    /// </summary>
    public bool IsStreaming { get; set; } = false;

    /// <summary>
    ///     平台类型：claude, gemini, openai
    /// </summary>
    public string Platform { get; set; } = "claude";

    /// <summary>
    ///     请求的年月日（便于按日期统计）
    /// </summary>
    public DateTime RequestDate { get; set; }

    /// <summary>
    ///     请求的小时（便于按小时统计）
    /// </summary>
    public int RequestHour { get; set; }

    /// <summary>
    ///     额外的元数据（JSON格式存储）
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    ///     导航属性 - 关联用户
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    ///     导航属性 - 关联API Key
    /// </summary>
    public ApiKey ApiKey { get; set; } = null!;

    /// <summary>
    ///     导航属性 - 关联钱包交易（如果有扣费）
    /// </summary>
    public WalletTransaction? WalletTransaction { get; set; }

    /// <summary>
    ///     计算总Token数量
    /// </summary>
    public void CalculateTotalTokens()
    {
        TotalTokens = InputTokens + OutputTokens + CacheCreateTokens + CacheReadTokens;
    }

    /// <summary>
    ///     设置请求完成信息
    /// </summary>
    public void CompleteRequest(DateTime endTime, string status = "success", string? errorMessage = null)
    {
        RequestEndTime = endTime;
        Status = status;
        ErrorMessage = errorMessage;
        DurationMs = (long)(endTime - RequestStartTime).TotalMilliseconds;
    }

    /// <summary>
    ///     初始化请求日期相关字段
    /// </summary>
    public void InitializeTimeFields()
    {
        RequestDate = RequestStartTime.Date;
        RequestHour = RequestStartTime.Hour;
    }
}