using ClaudeCodeProxy.Host.Models;
using ClaudeCodeProxy.Host.Services;

namespace ClaudeCodeProxy.Host.Endpoints;

/// <summary>
///     价格管理相关端点
/// </summary>
public static class PricingEndpoints
{
    /// <summary>
    ///     配置价格管理相关端点
    /// </summary>
    public static void MapPricingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/pricing")
            .WithTags("Pricing")
            .WithOpenApi()
            .RequireAuthorization();

        // 获取所有模型价格配置
        group.MapGet("models", GetAllModelPricing)
            .WithName("GetAllModelPricing")
            .WithSummary("获取所有模型价格配置")
            .Produces<ApiResponse<IEnumerable<ModelPricing>>>();

        // 更新模型价格配置
        group.MapPut("models", UpdateModelPricing)
            .WithName("UpdateModelPricing")
            .WithSummary("更新模型价格配置")
            .Produces<ApiResponse<object>>()
            .Produces(400);

        // 启用模型
        group.MapPost("models/{modelName}/enable", EnableModel)
            .WithName("EnableModel")
            .WithSummary("启用指定模型")
            .Produces<ApiResponse<object>>()
            .Produces(400);

        // 禁用模型
        group.MapPost("models/{modelName}/disable", DisableModel)
            .WithName("DisableModel")
            .WithSummary("禁用指定模型")
            .Produces<ApiResponse<object>>()
            .Produces(400);

        // 获取所有汇率配置
        group.MapGet("exchange-rates", GetAllExchangeRates)
            .WithName("GetAllExchangeRates")
            .WithSummary("获取所有汇率配置")
            .Produces<ApiResponse<IEnumerable<ExchangeRate>>>();

        // 更新汇率
        group.MapPut("exchange-rates", UpdateExchangeRate)
            .WithName("UpdateExchangeRate")
            .WithSummary("更新汇率")
            .Produces<ApiResponse<object>>()
            .Produces(400);

        // 计算费用预览
        group.MapPost("calculate", CalculateCost)
            .WithName("CalculateCost")
            .WithSummary("计算费用预览")
            .Produces<ApiResponse<PricingResult>>()
            .Produces(400);

        // 货币转换
        group.MapPost("convert-currency", ConvertCurrency)
            .WithName("ConvertCurrency")
            .WithSummary("货币转换")
            .Produces<ApiResponse<CurrencyConversionResult>>()
            .Produces(400);
    }

    /// <summary>
    ///     获取所有模型价格配置
    /// </summary>
    private static IResult GetAllModelPricing(PricingService pricingService)
    {
        var models = pricingService.GetAllModelPricing();
        return Results.Ok(new ApiResponse<IEnumerable<ModelPricing>>
        {
            Data = models,
            Message = "获取模型价格配置成功"
        });
    }

    /// <summary>
    ///     更新模型价格配置
    /// </summary>
    private static async Task<IResult> UpdateModelPricing(ModelPricing pricing, PricingService pricingService)
    {
        if (string.IsNullOrEmpty(pricing.Model))
            return Results.BadRequest(new ApiResponse<object>
            {
                Message = "模型名称不能为空"
            });

        try
        {
            await pricingService.UpdateModelPricingAsync(pricing);
            return Results.Ok(new ApiResponse<object>
            {
                Message = $"模型 {pricing.Model} 的价格配置已更新"
            });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new ApiResponse<object>
            {
                Message = $"更新模型价格配置失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    ///     启用模型
    /// </summary>
    private static async Task<IResult> EnableModel(string modelName, PricingService pricingService)
    {
        if (string.IsNullOrEmpty(modelName))
            return Results.BadRequest(new ApiResponse<object>
            {
                Message = "模型名称不能为空"
            });

        try
        {
            await pricingService.SetModelEnabledAsync(modelName);
            return Results.Ok(new ApiResponse<object>
            {
                Message = $"模型 {modelName} 已启用"
            });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new ApiResponse<object>
            {
                Message = $"启用模型失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    ///     禁用模型
    /// </summary>
    private static async Task<bool> DisableModel(string modelName, PricingService pricingService)
    {
        if (string.IsNullOrEmpty(modelName)) return false;

        await pricingService.SetModelEnabledAsync(modelName);
        return true;
    }

    /// <summary>
    ///     获取所有汇率配置
    /// </summary>
    private static IResult GetAllExchangeRates(PricingService pricingService)
    {
        var rates = pricingService.GetAllExchangeRates();
        return Results.Ok(new ApiResponse<IEnumerable<ExchangeRate>>
        {
            Data = rates,
            Message = "获取汇率配置成功"
        });
    }

    /// <summary>
    ///     更新汇率
    /// </summary>
    private static IResult UpdateExchangeRate(UpdateExchangeRateRequest request, PricingService pricingService)
    {
        if (string.IsNullOrEmpty(request.FromCurrency) || string.IsNullOrEmpty(request.ToCurrency))
            return Results.BadRequest(new ApiResponse<object>
            {
                Message = "货币类型不能为空"
            });

        if (request.Rate <= 0)
            return Results.BadRequest(new ApiResponse<object>
            {
                Message = "汇率必须大于0"
            });

        pricingService.UpdateExchangeRate(request.FromCurrency, request.ToCurrency, request.Rate);

        return Results.Ok(new ApiResponse<object>
        {
            Message = "汇率更新成功"
        });
    }

    /// <summary>
    ///     计算费用预览
    /// </summary>
    private static IResult CalculateCost(CalculateCostRequest request, PricingService pricingService)
    {
        if (string.IsNullOrEmpty(request.Model))
            return Results.BadRequest(new ApiResponse<object>
            {
                Message = "模型名称不能为空"
            });

        var totalCost = pricingService.CalculateTokenCost(
            request.Model,
            request.InputTokens,
            request.OutputTokens,
            request.CacheCreateTokens,
            request.CacheReadTokens);

        // 后端统一返回USD金额，前端根据需要进行汇率转换
        var result = new PricingResult
        {
            Model = request.Model,
            TotalCost = totalCost,
            Currency = "USD", // 后端统一返回USD
            WeightedTokens = request.InputTokens + request.OutputTokens + request.CacheCreateTokens +
                             request.CacheReadTokens,
            UnitPrice = totalCost / Math.Max(1,
                request.InputTokens + request.OutputTokens + request.CacheCreateTokens + request.CacheReadTokens)
        };

        return Results.Ok(new ApiResponse<PricingResult>
        {
            Data = result,
            Message = "费用计算成功"
        });
    }

    /// <summary>
    ///     货币转换
    /// </summary>
    private static IResult ConvertCurrency(CurrencyConversionRequest request, PricingService pricingService)
    {
        if (request.Amount <= 0)
            return Results.BadRequest(new ApiResponse<object>
            {
                Message = "转换金额必须大于0"
            });

        if (string.IsNullOrEmpty(request.FromCurrency) || string.IsNullOrEmpty(request.ToCurrency))
            return Results.BadRequest(new ApiResponse<object>
            {
                Message = "货币类型不能为空"
            });

        var convertedAmount = pricingService.ConvertCurrency(request.Amount, request.FromCurrency, request.ToCurrency);
        var exchangeRate = pricingService.GetExchangeRate(request.FromCurrency, request.ToCurrency);

        var result = new CurrencyConversionResult
        {
            OriginalAmount = request.Amount,
            FromCurrency = request.FromCurrency,
            ConvertedAmount = convertedAmount,
            ToCurrency = request.ToCurrency,
            ExchangeRate = exchangeRate?.Rate ?? 1.0m,
            UpdatedAt = exchangeRate?.UpdatedAt ?? DateTime.Now
        };

        return Results.Ok(new ApiResponse<CurrencyConversionResult>
        {
            Data = result,
            Message = "货币转换成功"
        });
    }
}

/// <summary>
///     更新汇率请求
/// </summary>
public class UpdateExchangeRateRequest
{
    public string FromCurrency { get; set; } = string.Empty;
    public string ToCurrency { get; set; } = string.Empty;
    public decimal Rate { get; set; }
}

/// <summary>
///     计算费用请求
/// </summary>
public class CalculateCostRequest
{
    public string Model { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int CacheCreateTokens { get; set; }
    public int CacheReadTokens { get; set; }
    public string? TargetCurrency { get; set; } = "USD";
}

/// <summary>
///     货币转换请求
/// </summary>
public class CurrencyConversionRequest
{
    public decimal Amount { get; set; }
    public string FromCurrency { get; set; } = "USD";
    public string ToCurrency { get; set; } = "CNY";
}

/// <summary>
///     货币转换结果
/// </summary>
public class CurrencyConversionResult
{
    public decimal OriginalAmount { get; set; }
    public string FromCurrency { get; set; } = string.Empty;
    public decimal ConvertedAmount { get; set; }
    public string ToCurrency { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public DateTime UpdatedAt { get; set; }
}