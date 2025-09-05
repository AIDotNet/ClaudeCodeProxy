using System.Collections.Concurrent;
using ClaudeCodeProxy.Core;
using ClaudeCodeProxy.Host.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaudeCodeProxy.Host.Services;

/// <summary>
///     价格管理服务
/// </summary>
public class PricingService
{
    private readonly ConcurrentDictionary<string, ExchangeRate> _exchangeRates = new();
    private readonly ILogger<PricingService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public PricingService(ILogger<PricingService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        InitializeDefaultPricing();
        InitializeDefaultExchangeRates();
    }

    /// <summary>
    ///     初始化默认价格配置 - 不需要，直接在计算时使用switch语句
    /// </summary>
    private void InitializeDefaultPricing()
    {
        // 移除所有模型定义，直接在计算方法中使用switch语句
        _logger.LogInformation("使用内置switch语句进行价格计算");
    }

    /// <summary>
    ///     初始化默认汇率配置
    /// </summary>
    private void InitializeDefaultExchangeRates()
    {
        // 默认汇率（这些应该定期更新）
        var rates = new[]
        {
            new ExchangeRate { FromCurrency = "CNY", ToCurrency = "USD", Rate = 0.1389m, UpdatedAt = DateTime.Now },
            new ExchangeRate { FromCurrency = "USD", ToCurrency = "CNY", Rate = 7.20m, UpdatedAt = DateTime.Now },
            new ExchangeRate { FromCurrency = "USD", ToCurrency = "USD", Rate = 1.00m, UpdatedAt = DateTime.Now },
            new ExchangeRate { FromCurrency = "CNY", ToCurrency = "CNY", Rate = 1.00m, UpdatedAt = DateTime.Now }
        };

        foreach (var rate in rates)
        {
            var key = $"{rate.FromCurrency}-{rate.ToCurrency}";
            _exchangeRates[key] = rate;
        }

        _logger.LogInformation("已加载 {Count} 个汇率配置", _exchangeRates.Count);
    }

    /// <summary>
    ///     计算Token费用 - 始终以USD为基准计算，汇率转换由前端显示时处理
    /// </summary>
    public decimal CalculateTokenCost(string model, int inputTokens, int outputTokens,
        int cacheCreateTokens = 0, int cacheReadTokens = 0)
    {
        try
        {
            // 优先从数据库获取定价信息
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IContext>();

            var dbPricing = context.ModelPricings
                .FirstOrDefault(p => p.Model == model && p.IsEnabled);

            ModelPricing pricing;
            if (dbPricing != null)
            {
                // 使用数据库中的定价
                pricing = new ModelPricing
                {
                    Model = dbPricing.Model,
                    InputPrice = dbPricing.InputPrice,
                    OutputPrice = dbPricing.OutputPrice,
                    CacheWritePrice = dbPricing.CacheWritePrice,
                    CacheReadPrice = dbPricing.CacheReadPrice,
                    Currency = "USD", // 后端统一使用USD计算
                    Description = dbPricing.Description
                };
            }
            else
            {
                // 降级使用静态数据
                pricing = ModelPricing.GetModelPricing(model);
                _logger.LogWarning("模型 {Model} 在数据库中未找到定价信息，使用默认定价", model);
            }

            // 分别计算各项费用 - 统一以USD计算
            var inputCost = inputTokens * pricing.InputPrice;
            var outputCost = outputTokens * pricing.OutputPrice;
            var cacheWriteCost = cacheCreateTokens * pricing.CacheWritePrice;
            var cacheReadCost = cacheReadTokens * pricing.CacheReadPrice;
            var totalCostUSD = inputCost + outputCost + cacheWriteCost + cacheReadCost;

            // 详细日志记录
            _logger.LogInformation(
                "价格计算详情 - 模型={Model}, 输入={Input:F9}, 输出={Output:F9}, 缓存写={CacheWrite:F9}, 缓存读={CacheRead:F9}",
                model, pricing.InputPrice, pricing.OutputPrice, pricing.CacheWritePrice, pricing.CacheReadPrice);
            _logger.LogInformation(
                "费用计算(USD) - 输入:{InputTokens}*{InputPrice:F9}={InputCost:F6}, 输出:{OutputTokens}*{OutputPrice:F9}={OutputCost:F6}, 缓存写:{CacheCreate}*{CacheWritePrice:F9}={CacheWriteCost:F6}, 缓存读:{CacheRead}*{CacheReadPrice:F9}={CacheReadCost:F6}, 总计:{TotalCost:F6}",
                inputTokens, pricing.InputPrice, inputCost, outputTokens, pricing.OutputPrice, outputCost,
                cacheCreateTokens, pricing.CacheWritePrice, cacheWriteCost, cacheReadTokens, pricing.CacheReadPrice,
                cacheReadCost, totalCostUSD);

            return totalCostUSD; // 统一返回USD金额
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "计算Token费用时发生错误，使用默认定价");
            // 发生错误时降级使用静态数据
            var pricing = ModelPricing.GetModelPricing(model);
            return inputTokens * pricing.InputPrice + outputTokens * pricing.OutputPrice +
                   cacheCreateTokens * pricing.CacheWritePrice + cacheReadTokens * pricing.CacheReadPrice;
        }
    }

    /// <summary>
    ///     获取汇率 - 用于前端显示时的货币转换
    /// </summary>
    public ExchangeRate? GetExchangeRate(string fromCurrency, string toCurrency)
    {
        var key = $"{fromCurrency}-{toCurrency}";
        return _exchangeRates.TryGetValue(key, out var rate) ? rate : null;
    }

    /// <summary>
    ///     转换货币金额 - 用于前端显示
    /// </summary>
    public decimal ConvertCurrency(decimal amount, string fromCurrency, string toCurrency)
    {
        if (fromCurrency == toCurrency)
            return amount;

        var rate = GetExchangeRate(fromCurrency, toCurrency);
        if (rate == null)
        {
            _logger.LogWarning("未找到汇率: {FromCurrency} -> {ToCurrency}，返回原金额", fromCurrency, toCurrency);
            return amount;
        }

        var convertedAmount = amount * rate.Rate;
        _logger.LogDebug("货币转换: {Amount} {FromCurrency} -> {ConvertedAmount} {ToCurrency} (汇率: {Rate})",
            amount, fromCurrency, convertedAmount, toCurrency, rate.Rate);

        return convertedAmount;
    }

    /// <summary>
    ///     更新汇率
    /// </summary>
    public void UpdateExchangeRate(string fromCurrency, string toCurrency, decimal rate)
    {
        var key = $"{fromCurrency}-{toCurrency}";
        var exchangeRate = new ExchangeRate
        {
            FromCurrency = fromCurrency,
            ToCurrency = toCurrency,
            Rate = rate,
            UpdatedAt = DateTime.Now
        };

        _exchangeRates[key] = exchangeRate;
        _logger.LogInformation("已更新汇率: {FromCurrency} -> {ToCurrency} = {Rate}",
            fromCurrency, toCurrency, rate);
    }

    /// <summary>
    ///     获取所有支持的模型 - 基于switch语句
    /// </summary>
    public IEnumerable<string> GetAllSupportedModels()
    {
        return new[]
        {
            "claude-3-5-sonnet-20241022",
            "claude-3-5-haiku-20241022",
            "claude-3-opus-20240229"
        };
    }

    /// <summary>
    ///     获取所有模型价格信息 - 用于API返回（包含所有模型，无论启用状态）
    /// </summary>
    public IEnumerable<ModelPricing> GetAllModelPricing()
    {
        try
        {
            // 优先从数据库获取所有模型（包括禁用的）
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IContext>();

            var dbPricings = context.ModelPricings.ToList();

            if (dbPricings.Any())
                return dbPricings.Select(p => new ModelPricing
                {
                    Model = p.Model,
                    InputPrice = p.InputPrice,
                    OutputPrice = p.OutputPrice,
                    CacheWritePrice = p.CacheWritePrice,
                    CacheReadPrice = p.CacheReadPrice,
                    Currency = p.Currency,
                    Description = p.Description,
                    IsEnabled = p.IsEnabled // 添加启用状态字段
                }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从数据库获取模型定价信息时发生错误，使用默认数据");
        }

        // 降级使用静态数据
        return ModelPricing.AllModels.Select(p => new ModelPricing
        {
            Model = p.Model,
            InputPrice = p.InputPrice,
            OutputPrice = p.OutputPrice,
            CacheWritePrice = p.CacheWritePrice,
            CacheReadPrice = p.CacheReadPrice,
            Currency = p.Currency,
            Description = p.Description,
            IsEnabled = true // 静态数据默认启用
        });
    }

    /// <summary>
    ///     更新模型价格配置
    /// </summary>
    public async Task UpdateModelPricingAsync(ModelPricing pricing)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IContext>();

        var existingPricing = await context.ModelPricings
            .FirstOrDefaultAsync(p => p.Model == pricing.Model);

        if (existingPricing != null)
        {
            // 更新现有记录
            existingPricing.InputPrice = pricing.InputPrice;
            existingPricing.OutputPrice = pricing.OutputPrice;
            existingPricing.CacheWritePrice = pricing.CacheWritePrice;
            existingPricing.CacheReadPrice = pricing.CacheReadPrice;
            existingPricing.Currency = pricing.Currency;
            existingPricing.Description = pricing.Description;
            existingPricing.IsEnabled = pricing.IsEnabled;
            existingPricing.ModifiedAt = DateTime.UtcNow;
        }
        else
        {
            // 创建新记录
            context.ModelPricings.Add(new Domain.ModelPricing
            {
                Id = Guid.NewGuid(),
                Model = pricing.Model,
                InputPrice = pricing.InputPrice,
                OutputPrice = pricing.OutputPrice,
                CacheWritePrice = pricing.CacheWritePrice,
                CacheReadPrice = pricing.CacheReadPrice,
                Currency = pricing.Currency,
                Description = pricing.Description,
                IsEnabled = pricing.IsEnabled,
                CreatedAt = DateTime.UtcNow
            });
        }

        await context.SaveAsync();
        _logger.LogInformation("模型 {Model} 的价格配置已更新，启用状态: {IsEnabled}", pricing.Model, pricing.IsEnabled);
    }

    /// <summary>
    ///     设置模型启用状态
    /// </summary>
    public async Task SetModelEnabledAsync(string modelName)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IContext>();

        var modelPricing = await context.ModelPricings
            .FirstOrDefaultAsync(x => x.Model == modelName);

        var value = !modelPricing!.IsEnabled;

        await context.ModelPricings.Where(x => x.Model == modelName)
            .ExecuteUpdateAsync(x =>
                x.SetProperty(p => p.IsEnabled, value)
                    .SetProperty(p => p.ModifiedAt, DateTime.UtcNow));
    }

    /// <summary>
    ///     获取所有汇率
    /// </summary>
    public IEnumerable<ExchangeRate> GetAllExchangeRates()
    {
        return _exchangeRates.Values.OrderBy(x => x.FromCurrency).ThenBy(x => x.ToCurrency);
    }
}