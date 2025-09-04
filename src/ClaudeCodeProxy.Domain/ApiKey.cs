namespace ClaudeCodeProxy.Domain;

/// <summary>
/// API Key 实体类
/// </summary>
public class ApiKey : Entity<Guid>
{
    /// <summary>
    /// 用户ID - 每个API Key都属于一个用户
    /// </summary>
    public Guid UserId { get; set; }
    /// <summary>
    /// API Key 名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// API Key 值（加密存储）
    /// </summary>
    public string KeyValue { get; set; } = string.Empty;

    /// <summary>
    /// 备注描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 标签列表（用于分类管理）
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Token 限制（窗口内最大Token数）
    /// </summary>
    public int? TokenLimit { get; set; }

    /// <summary>
    /// 速率限制时间窗口（分钟）
    /// </summary>
    public int? RateLimitWindow { get; set; }

    /// <summary>
    /// 请求次数限制（窗口内最大请求数）
    /// </summary>
    public int? RateLimitRequests { get; set; }

    /// <summary>
    /// 并发限制（同时处理的最大请求数，0表示无限制）
    /// </summary>
    public int ConcurrencyLimit { get; set; } = 0;

    /// <summary>
    /// 每日费用限制（美元，0表示无限制）
    /// </summary>
    public decimal DailyCostLimit { get; set; } = 0;

    /// <summary>
    /// 月度费用限制（美元，0表示无限制）
    /// </summary>
    public decimal MonthlyCostLimit { get; set; } = 0;

    /// <summary>
    /// 总费用限制（美元，0表示无限制）
    /// </summary>
    public decimal TotalCostLimit { get; set; } = 0;

    /// <summary>
    /// 当日已使用费用（美元）
    /// </summary>
    public decimal DailyCostUsed { get; set; } = 0;

    /// <summary>
    /// 当月已使用费用（美元）
    /// </summary>
    public decimal MonthlyCostUsed { get; set; } = 0;

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// 服务权限（all=全部服务，claude=仅Claude，gemini=仅Gemini）
    /// </summary>
    public string Permissions { get; set; } = "all";

    /// <summary>
    /// 绑定的 Claude OAuth 账户ID
    /// </summary>
    public string? ClaudeAccountId { get; set; }

    /// <summary>
    /// 绑定的 Claude Console 账户ID
    /// </summary>
    public string? ClaudeConsoleAccountId { get; set; }

    /// <summary>
    /// 绑定的 Gemini 账户ID
    /// </summary>
    public string? GeminiAccountId { get; set; }

    /// <summary>
    /// 是否启用模型限制
    /// </summary>
    public bool EnableModelRestriction { get; set; } = false;

    /// <summary>
    /// 限制的模型列表
    /// </summary>
    public List<string>? RestrictedModels { get; set; }

    /// <summary>
    /// 是否启用客户端限制
    /// </summary>
    public bool EnableClientRestriction { get; set; } = false;

    /// <summary>
    /// 允许的客户端列表
    /// </summary>
    public List<string>? AllowedClients { get; set; }

    /// <summary>
    /// 是否已启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 最后使用时间
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// 总使用次数
    /// </summary>
    public long TotalUsageCount { get; set; } = 0;

    /// <summary>
    /// 总消费金额
    /// </summary>
    public decimal TotalCost { get; set; } = 0;

    /// <summary>
    /// 如果设置了模型，则使用此模型进行请求
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// 服务类型
    /// </summary>
    /// <returns></returns>
    public string Service { get; set; } = "claude";

    /// <summary>
    /// 默认绑定账户ID（优先使用此账户）
    /// </summary>
    public string? DefaultAccountId { get; set; }

    /// <summary>
    /// API Key绑定的账户优先级列表（JSON格式存储账户ID和优先级映射）
    /// 格式：[{"AccountId": "xxx", "Priority": 1}, {"AccountId": "yyy", "Priority": 2}]
    /// </summary>
    public List<ApiKeyAccountBinding>? AccountBindings { get; set; }

    /// <summary>
    /// 导航属性 - 关联用户
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// 导航属性 - 关联的请求日志
    /// </summary>
    public virtual ICollection<RequestLog> RequestLogs { get; set; } = new List<RequestLog>();

    public bool IsClaude()
    {
        return Service.Equals("claude", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsGemini()
    {
        return Service.Equals("gemini", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsOpenAI()
    {
        return Service.Equals("openai", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 检查API Key是否有效
    /// </summary>
    public bool IsValid()
    {
        return IsEnabled && (ExpiresAt == null || ExpiresAt > DateTime.Now);
    }

    /// <summary>
    /// 检查是否可以访问指定服务
    /// </summary>
    public bool CanAccessService(string service)
    {
        return Permissions == "all" || Permissions == service;
    }

    /// <summary>
    /// 检查是否可以使用指定模型
    /// </summary>
    public bool CanUseModel(string model)
    {
        if (!EnableModelRestriction || RestrictedModels == null || RestrictedModels.Count == 0)
            return true;

        return !RestrictedModels.Contains(model);
    }

    /// <summary>
    /// 检查是否允许指定客户端
    /// </summary>
    public bool IsClientAllowed(string clientId)
    {
        if (!EnableClientRestriction || AllowedClients == null || AllowedClients.Count == 0)
            return true;

        return AllowedClients.Contains(clientId);
    }

    /// <summary>
    /// 检查是否超过费用限制
    /// </summary>
    /// <param name="additionalCost">计划增加的费用</param>
    /// <returns>超过限制的类型，null表示没有超过限制</returns>
    public string? CheckCostLimit(decimal additionalCost = 0)
    {
        // 检查每日费用限制
        if (DailyCostLimit > 0 && (DailyCostUsed + additionalCost) > DailyCostLimit)
        {
            return "daily";
        }

        // 检查月度费用限制
        if (MonthlyCostLimit > 0 && (MonthlyCostUsed + additionalCost) > MonthlyCostLimit)
        {
            return "monthly";
        }

        // 检查总费用限制
        if (TotalCostLimit > 0 && (TotalCost + additionalCost) > TotalCostLimit)
        {
            return "total";
        }

        return null;
    }

    /// <summary>
    /// 检查是否接近费用限制（超过80%）
    /// </summary>
    /// <param name="threshold">警告阈值，默认0.8（80%）</param>
    /// <returns>接近限制的类型，null表示没有接近限制</returns>
    public string? CheckCostWarning(decimal threshold = 0.8m)
    {
        // 检查每日费用警告
        if (DailyCostLimit > 0 && DailyCostUsed > (DailyCostLimit * threshold))
        {
            return "daily";
        }

        // 检查月度费用警告
        if (MonthlyCostLimit > 0 && MonthlyCostUsed > (MonthlyCostLimit * threshold))
        {
            return "monthly";
        }

        // 检查总费用警告
        if (TotalCostLimit > 0 && TotalCost > (TotalCostLimit * threshold))
        {
            return "total";
        }

        return null;
    }

    /// <summary>
    /// 获取费用使用率
    /// </summary>
    /// <returns>费用使用率信息</returns>
    public CostUsageInfo GetCostUsage()
    {
        return new CostUsageInfo
        {
            DailyUsage = DailyCostLimit > 0 ? DailyCostUsed / DailyCostLimit : 0,
            MonthlyUsage = MonthlyCostLimit > 0 ? MonthlyCostUsed / MonthlyCostLimit : 0,
            TotalUsage = TotalCostLimit > 0 ? TotalCost / TotalCostLimit : 0,
            DailyCostUsed = DailyCostUsed,
            DailyCostLimit = DailyCostLimit,
            MonthlyCostUsed = MonthlyCostUsed,
            MonthlyCostLimit = MonthlyCostLimit,
            TotalCostUsed = TotalCost,
            TotalCostLimit = TotalCostLimit
        };
    }

    /// <summary>
    /// 获取绑定的账户ID列表（按优先级排序）
    /// </summary>
    /// <returns>按优先级排序的账户ID列表</returns>
    public List<string> GetBoundAccountIds()
    {
        var accountIds = new List<string>();
        
        // 首先添加默认账户
        if (!string.IsNullOrEmpty(DefaultAccountId))
        {
            accountIds.Add(DefaultAccountId);
        }
        
        // 添加绑定的账户（按优先级排序，排除默认账户）
        if (AccountBindings is { Count: > 0 })
        {
            var sortedBindings = AccountBindings
                .Where(b => b.IsEnabled && b.AccountId != DefaultAccountId)
                .OrderBy(b => b.Priority)
                .Select(b => b.AccountId)
                .ToList();
                
            accountIds.AddRange(sortedBindings);
        }
        
        return accountIds;
    }

    /// <summary>
    /// 检查是否绑定了指定账户
    /// </summary>
    /// <param name="accountId">账户ID</param>
    /// <returns>是否绑定了该账户</returns>
    public bool IsBoundToAccount(string accountId)
    {
        if (DefaultAccountId == accountId)
        {
            return true;
        }
        
        return AccountBindings?.Any(b => b.IsEnabled && b.AccountId == accountId) == true;
    }
}

/// <summary>
/// 费用使用率信息
/// </summary>
public class CostUsageInfo
{
    public decimal DailyUsage { get; set; }
    public decimal MonthlyUsage { get; set; }
    public decimal TotalUsage { get; set; }
    public decimal DailyCostUsed { get; set; }
    public decimal DailyCostLimit { get; set; }
    public decimal MonthlyCostUsed { get; set; }
    public decimal MonthlyCostLimit { get; set; }
    public decimal TotalCostUsed { get; set; }
    public decimal TotalCostLimit { get; set; }
}

/// <summary>
/// API Key账户绑定关系
/// </summary>
public class ApiKeyAccountBinding
{
    /// <summary>
    /// 账户ID
    /// </summary>
    public string AccountId { get; set; } = string.Empty;
    
    /// <summary>
    /// 优先级 (1-100)，数字越小优先级越高
    /// </summary>
    public int Priority { get; set; } = 50;
    
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}