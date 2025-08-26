using System.Text.Json.Serialization;

namespace ClaudeCodeProxy.Host.Models;

/// <summary>
/// 模型价格信息
/// </summary>
public class ModelPricing
{
    /// <summary>
    /// 模型名称
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// 输入token单价
    /// </summary>
    public decimal InputPrice { get; set; }

    /// <summary>
    /// 输出token单价
    /// </summary>
    public decimal OutputPrice { get; set; }

    /// <summary>
    /// 缓存写入单价
    /// </summary>
    public decimal CacheWritePrice { get; set; }

    /// <summary>
    /// 缓存读取单价
    /// </summary>
    public decimal CacheReadPrice { get; set; }

    /// <summary>
    /// 货币类型
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// 价格描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 获取所有模型的定价数据
    /// </summary>
    public static readonly List<ModelPricing> AllModels = new()
    {
        new ModelPricing
        {
            Model = "claude-3-5-sonnet-20241022",
            InputPrice = 0.000003m,
            OutputPrice = 0.000015m,
            CacheWritePrice = 0.00000375m,
            CacheReadPrice = 0.0000003m,
            Currency = "USD",
            Description = "Claude 3.5 Sonnet - $0.000003/输入, $0.000015/输出"
        },
        new ModelPricing
        {
            Model = "claude-3-5-haiku-20241022",
            InputPrice = 0.000001m,
            OutputPrice = 0.000005m,
            CacheWritePrice = 0.00000125m,
            CacheReadPrice = 0.0000001m,
            Currency = "USD",
            Description = "Claude 3.5 Haiku - $0.000001/输入, $0.000005/输出"
        },
        new ModelPricing
        {
            Model = "claude-3-opus-20240229",
            InputPrice = 0.000015m,
            OutputPrice = 0.000075m,
            CacheWritePrice = 0.00001875m,
            CacheReadPrice = 0.0000015m,
            Currency = "USD",
            Description = "Claude 3 Opus - $0.000015/输入, $0.000075/输出"
        },
        new ModelPricing
        {
            Model = "claude-sonnet-4-20250514",
            InputPrice = 0.000003m,
            OutputPrice = 0.000015m,
            CacheWritePrice = 0.00000375m,
            CacheReadPrice = 0.0000003m,
            Currency = "USD",
            Description = "Claude Sonnet 4 - $0.000003/输入, $0.000015/输出"
        },
        new ModelPricing
        {
            Model = "claude-opus-4-20250514",
            InputPrice = 0.000015m,
            OutputPrice = 0.000075m,
            CacheWritePrice = 0.00001875m,
            CacheReadPrice =  0.0000015m,
            Currency = "USD",
            Description = "Claude Opus 4 - $0.000015/输入, $0.000075/输出"
        },
        new ModelPricing
        {
            Model = "claude-opus-4-1-20250805",
            InputPrice = 0.000003m,
            OutputPrice = 0.000015m,
            CacheWritePrice = 0.00001875m,
            CacheReadPrice =  0.0000015m,
            Currency = "USD",
            Description = "Claude Opus 4.1 - $0.000003/输入, $0.000015/输出"
        },
        new ModelPricing
        {
            Model = "glm-4.5",
            InputPrice = 0.00000055m,
            OutputPrice = 0.0000022m,
            CacheWritePrice = 0,
            CacheReadPrice = 0,
            Currency = "USD",
            Description = "GLM 4.5 - $0.00000055/输入, $0.0000022/输出"
        },
        new ModelPricing
        {
            Model = "kimi-k2-0711-preview",
            InputPrice = 0.0000006m,
            OutputPrice = 0.0000025m,
            CacheReadPrice = 0.00000015m,
            Currency = "USD",
            Description = "Kimi K2 - $0.0000006/输入, $0.0000025/输出"
        },
        new ModelPricing
        {
            Model = "kimi-k2-250711",
            InputPrice = 0.0000006m,
            OutputPrice = 0.0000025m,
            CacheReadPrice = 0.00000015m,
            Currency = "USD",
            Description = "Kimi K2 - $0.0000006/输入, $0.0000025/输出"
        },
        new ModelPricing
        {
            Model = "gpt-5",
            InputPrice = 0.00000125m,
            OutputPrice = 0.00001m,
            CacheReadPrice = 0.0000015m,
            Currency = "USD",
            Description = "gpt-5 - $0.00125/输入, $0.01m/输出"
        }
    };

    /// <summary>
    /// 根据模型名称获取定价信息
    /// </summary>
    /// <param name="model">模型名称</param>
    /// <returns>模型定价信息，如果未找到则返回默认的Sonnet定价</returns>
    public static ModelPricing GetModelPricing(string model)
    {
        var pricing = AllModels.FirstOrDefault(m => m.Model == model);

        // 如果没找到，返回默认的Sonnet定价
        if (pricing == null)
        {
            pricing = AllModels.First(m => m.Model == "claude-3-5-sonnet-20241022");
        }

        return pricing;
    }
}

/// <summary>
/// 汇率配置
/// </summary>
public class ExchangeRate
{
    /// <summary>
    /// 源货币
    /// </summary>
    public string FromCurrency { get; set; } = string.Empty;

    /// <summary>
    /// 目标货币
    /// </summary>
    public string ToCurrency { get; set; } = string.Empty;

    /// <summary>
    /// 汇率
    /// </summary>
    public decimal Rate { get; set; }

    /// <summary>
    /// 汇率更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 价格计算结果
/// </summary>
public class PricingResult
{
    /// <summary>
    /// 总token数
    /// </summary>
    public decimal WeightedTokens { get; set; }

    /// <summary>
    /// 平均单价
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// 总费用
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// 货币类型
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// 使用的模型
    /// </summary>
    public string Model { get; set; } = string.Empty;
}