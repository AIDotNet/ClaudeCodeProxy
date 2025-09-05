using ClaudeCodeProxy.Core;
using ClaudeCodeProxy.Domain;

namespace ClaudeCodeProxy.Host.Services;

/// <summary>
///     模型定价初始化服务
/// </summary>
public class ModelPricingInitService
{
    private readonly IContext _context;
    private readonly ILogger<ModelPricingInitService> _logger;

    public ModelPricingInitService(IContext context, ILogger<ModelPricingInitService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    ///     初始化模型定价数据 - 兼容老版本数据库
    /// </summary>
    public async Task InitializeModelPricingAsync()
    {
        // 检查数据库中是否已有模型定价数据
        var existingModels = _context.ModelPricings.ToList();

        if (existingModels.Any())
        {
            _logger.LogInformation("模型定价数据已存在，跳过初始化。现有模型数量: {Count}", existingModels.Count);

            // 检查是否有新模型需要添加
            var existingModelNames = existingModels.Select(m => m.Model).ToHashSet();
            var newModels = Models.ModelPricing.AllModels.Where(m => !existingModelNames.Contains(m.Model)).ToList();

            if (newModels.Any())
            {
                _logger.LogInformation("发现 {Count} 个新模型，开始添加...", newModels.Count);
                foreach (var modelPricing in newModels)
                {
                    var entity = CreateModelPricingEntity(modelPricing);
                    _context.ModelPricings.Add(entity);
                    _logger.LogInformation("添加新模型定价: {Model}", entity.Model);
                }

                await _context.SaveAsync();
                _logger.LogInformation("新模型定价数据添加完成");
            }

            return;
        }

        _logger.LogInformation("开始初始化模型定价数据...");

        // 将静态数据转换为域实体并添加到数据库
        foreach (var modelPricing in Models.ModelPricing.AllModels)
        {
            var entity = CreateModelPricingEntity(modelPricing);
            _context.ModelPricings.Add(entity);
            _logger.LogInformation("添加模型定价: {Model} - 输入:{InputPrice}, 输出:{OutputPrice}",
                entity.Model, entity.InputPrice, entity.OutputPrice);
        }

        await _context.SaveAsync();
        _logger.LogInformation("模型定价数据初始化完成，共初始化 {Count} 个模型", Models.ModelPricing.AllModels.Count);
    }

    /// <summary>
    ///     创建模型定价实体的辅助方法
    /// </summary>
    private ModelPricing CreateModelPricingEntity(Models.ModelPricing modelPricing)
    {
        return new ModelPricing
        {
            Id = Guid.NewGuid(),
            Model = modelPricing.Model,
            InputPrice = modelPricing.InputPrice,
            OutputPrice = modelPricing.OutputPrice,
            CacheWritePrice = modelPricing.CacheWritePrice,
            CacheReadPrice = modelPricing.CacheReadPrice,
            Currency = modelPricing.Currency,
            Description = modelPricing.Description,
            IsEnabled = true,
            CreatedAt = DateTime.Now
        };
    }
}