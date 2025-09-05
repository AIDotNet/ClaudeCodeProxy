namespace ClaudeCodeProxy.Host.Models;

/// <summary>
///     统一API响应模型
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    ///     是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    ///     响应消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    ///     响应数据
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    ///     错误代码
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    ///     时间戳
    /// </summary>
    public long Timestamp { get; set; } = DateTimeOffset.Now.ToUnixTimeSeconds();

    /// <summary>
    ///     创建成功响应
    /// </summary>
    public static ApiResponse<T> Ok(T? data = default, string message = "操作成功")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    /// <summary>
    ///     创建失败响应
    /// </summary>
    public static ApiResponse<T> Fail(string message, string? errorCode = null, T? data = default)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode,
            Data = data
        };
    }
}

/// <summary>
///     无数据的统一API响应模型
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    /// <summary>
    ///     创建成功响应
    /// </summary>
    public new static ApiResponse Ok(string message = "操作成功")
    {
        return new ApiResponse
        {
            Success = true,
            Message = message
        };
    }

    /// <summary>
    ///     创建失败响应
    /// </summary>
    public new static ApiResponse Fail(string message, string? errorCode = null)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode
        };
    }
}

/// <summary>
///     分页结果模型
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class PagedResult<T>
{
    /// <summary>
    ///     数据列表
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    ///     总记录数
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    ///     当前页码（从1开始）
    /// </summary>
    public int PageIndex { get; set; }

    /// <summary>
    ///     每页大小
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    ///     总页数
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    ///     是否有上一页
    /// </summary>
    public bool HasPreviousPage => PageIndex > 1;

    /// <summary>
    ///     是否有下一页
    /// </summary>
    public bool HasNextPage => PageIndex < TotalPages;
}