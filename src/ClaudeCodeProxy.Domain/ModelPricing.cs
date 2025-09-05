namespace ClaudeCodeProxy.Domain;

/// <summary>
///     模型价格配置实体
/// </summary>
public sealed class ModelPricing : Entity<Guid>
{
    /// <summary>
    ///     模型名称
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    ///     输入token单价
    /// </summary>
    public decimal InputPrice { get; set; }

    /// <summary>
    ///     输出token单价
    /// </summary>
    public decimal OutputPrice { get; set; }

    /// <summary>
    ///     缓存写入单价
    /// </summary>
    public decimal CacheWritePrice { get; set; }

    /// <summary>
    ///     缓存读取单价
    /// </summary>
    public decimal CacheReadPrice { get; set; }

    /// <summary>
    ///     货币类型
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    ///     价格描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}