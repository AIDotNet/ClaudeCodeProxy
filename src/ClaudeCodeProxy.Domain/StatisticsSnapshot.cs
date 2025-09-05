namespace ClaudeCodeProxy.Domain;

/// <summary>
///     统计快照实体类
///     用于存储聚合的统计数据，提高查询性能
/// </summary>
public sealed class StatisticsSnapshot : Entity<Guid>
{
    /// <summary>
    ///     快照类型：daily, hourly, realtime
    /// </summary>
    public string SnapshotType { get; set; } = string.Empty;

    /// <summary>
    ///     快照日期（仅日期部分）
    /// </summary>
    public DateTime SnapshotDate { get; set; }

    /// <summary>
    ///     快照小时（当SnapshotType为hourly时使用）
    /// </summary>
    public int? SnapshotHour { get; set; }

    /// <summary>
    ///     用户ID（如果是特定用户的统计）
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    ///     API Key ID（如果是特定API Key的统计）
    /// </summary>
    public Guid? ApiKeyId { get; set; }

    /// <summary>
    ///     模型名称（如果是特定模型的统计）
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    ///     请求总数
    /// </summary>
    public long RequestCount { get; set; } = 0;

    /// <summary>
    ///     成功请求数
    /// </summary>
    public long SuccessfulRequestCount { get; set; } = 0;

    /// <summary>
    ///     失败请求数
    /// </summary>
    public long FailedRequestCount { get; set; } = 0;

    /// <summary>
    ///     输入Token总数
    /// </summary>
    public long InputTokens { get; set; } = 0;

    /// <summary>
    ///     输出Token总数
    /// </summary>
    public long OutputTokens { get; set; } = 0;

    /// <summary>
    ///     缓存创建Token总数
    /// </summary>
    public long CacheCreateTokens { get; set; } = 0;

    /// <summary>
    ///     缓存读取Token总数
    /// </summary>
    public long CacheReadTokens { get; set; } = 0;

    /// <summary>
    ///     总Token数
    /// </summary>
    public long TotalTokens { get; set; }

    /// <summary>
    ///     总费用（美元）
    /// </summary>
    public decimal TotalCost { get; set; } = 0;

    /// <summary>
    ///     平均响应时间（毫秒）
    /// </summary>
    public double? AverageResponseTime { get; set; }

    /// <summary>
    ///     最大响应时间（毫秒）
    /// </summary>
    public long? MaxResponseTime { get; set; }

    /// <summary>
    ///     最小响应时间（毫秒）
    /// </summary>
    public long? MinResponseTime { get; set; }

    /// <summary>
    ///     活跃API Key数量
    /// </summary>
    public int? ActiveApiKeyCount { get; set; }

    /// <summary>
    ///     活跃账户数量
    /// </summary>
    public int? ActiveAccountCount { get; set; }

    /// <summary>
    ///     限流账户数量
    /// </summary>
    public int? RateLimitedAccountCount { get; set; }

    /// <summary>
    ///     唯一用户数（根据IP统计）
    /// </summary>
    public int? UniqueUserCount { get; set; }

    /// <summary>
    ///     数据版本（用于并发控制）
    /// </summary>
    public long Version { get; set; } = 1;

    /// <summary>
    ///     导航属性 - 关联用户
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    ///     导航属性 - 关联API Key
    /// </summary>
    public ApiKey? ApiKey { get; set; }

    /// <summary>
    ///     计算总Token数量
    /// </summary>
    public void CalculateTotalTokens()
    {
        TotalTokens = InputTokens + OutputTokens + CacheCreateTokens + CacheReadTokens;
    }

    /// <summary>
    ///     计算成功率
    /// </summary>
    public double GetSuccessRate()
    {
        if (RequestCount == 0) return 0;
        return (double)SuccessfulRequestCount / RequestCount * 100;
    }

    /// <summary>
    ///     创建日统计快照
    /// </summary>
    public static StatisticsSnapshot CreateDailySnapshot(DateTime date)
    {
        return new StatisticsSnapshot
        {
            Id = Guid.NewGuid(),
            SnapshotType = "daily",
            SnapshotDate = date.Date,
            CreatedAt = DateTime.Now
        };
    }

    /// <summary>
    ///     创建小时统计快照
    /// </summary>
    public static StatisticsSnapshot CreateHourlySnapshot(DateTime dateTime)
    {
        return new StatisticsSnapshot
        {
            Id = Guid.NewGuid(),
            SnapshotType = "hourly",
            SnapshotDate = dateTime.Date,
            SnapshotHour = dateTime.Hour,
            CreatedAt = DateTime.Now
        };
    }

    /// <summary>
    ///     创建实时统计快照
    /// </summary>
    public static StatisticsSnapshot CreateRealtimeSnapshot()
    {
        var now = DateTime.Now;
        return new StatisticsSnapshot
        {
            Id = Guid.NewGuid(),
            SnapshotType = "realtime",
            SnapshotDate = now.Date,
            SnapshotHour = now.Hour,
            CreatedAt = now
        };
    }
}