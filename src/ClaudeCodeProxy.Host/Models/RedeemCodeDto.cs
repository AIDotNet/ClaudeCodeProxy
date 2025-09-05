namespace ClaudeCodeProxy.Host.Models;

/// <summary>
///     兑换码DTO
/// </summary>
public class RedeemCodeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public bool IsUsed { get; set; }
    public Guid? UsedByUserId { get; set; }
    public string? UsedByUserName { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsEnabled { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

/// <summary>
///     创建兑换码请求
/// </summary>
public class CreateRedeemCodeRequest
{
    /// <summary>
    ///     兑换码类型
    /// </summary>
    public string Type { get; set; } = "balance";

    /// <summary>
    ///     兑换金额
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    ///     描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     过期时间
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    ///     生成数量
    /// </summary>
    public int Count { get; set; } = 1;
}

/// <summary>
///     使用兑换码请求
/// </summary>
public class UseRedeemCodeRequest
{
    /// <summary>
    ///     兑换码
    /// </summary>
    public string Code { get; set; } = string.Empty;
}

/// <summary>
///     兑换码使用结果
/// </summary>
public class RedeemCodeUseResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal NewBalance { get; set; }
}

/// <summary>
///     兑换码列表请求
/// </summary>
public class RedeemCodeListRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Code { get; set; }
    public string? Type { get; set; }
    public bool? IsUsed { get; set; }
    public bool? IsEnabled { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UsedByUserId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string SortBy { get; set; } = "CreatedAt";
    public string SortDirection { get; set; } = "desc";
}

/// <summary>
///     兑换码列表响应
/// </summary>
public class RedeemCodeListResponse
{
    public List<RedeemCodeDto> Data { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
///     兑换记录DTO（用户视角）
/// </summary>
public class RedeemRecordDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime UsedAt { get; set; }
}