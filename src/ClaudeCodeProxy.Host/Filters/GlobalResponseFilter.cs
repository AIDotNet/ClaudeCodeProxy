using System.Text.Json;
using ClaudeCodeProxy.Host.Models;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ClaudeCodeProxy.Host.Filters;

/// <summary>
///     全局响应过滤器
/// </summary>
public class GlobalResponseFilter(ILogger<GlobalResponseFilter> logger) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context);

        // 如果已经是ApiResponse类型，直接返回
        if (result is ApiResponse<object> or ApiResponse) return result;

        // 如果是IResult类型，检查是否需要包装
        if (result is IResult httpResult) return await HandleHttpResult(httpResult, context);

        // 包装普通对象为成功响应
        if (result != null) return ApiResponse<object>.Ok(result);

        return ApiResponse.Ok();
    }

    private async Task<object> HandleHttpResult(IResult httpResult, EndpointFilterInvocationContext context)
    {
        return httpResult switch
        {
            // 处理不同类型的HTTP结果
            Ok<object> okResult => ApiResponse<object>.Ok(okResult.Value),
            Ok okEmptyResult => ApiResponse.Ok(),
            BadRequest<object> badRequestResult => ApiResponse<object>.Fail("请求参数错误", "BAD_REQUEST",
                badRequestResult.Value),
            BadRequest badRequestEmptyResult => ApiResponse.Fail("请求参数错误", "BAD_REQUEST"),
            NotFound<object> notFoundResult => ApiResponse<object>.Fail("资源未找到", "NOT_FOUND", notFoundResult.Value),
            NotFound notFoundEmptyResult => ApiResponse.Fail("资源未找到", "NOT_FOUND"),
            UnauthorizedHttpResult => ApiResponse.Fail("未授权访问", "UNAUTHORIZED"),
            ForbidHttpResult => ApiResponse.Fail("禁止访问", "FORBIDDEN"),
            Conflict<object> conflictResult => ApiResponse<object>.Fail("资源冲突", "CONFLICT", conflictResult.Value),
            Conflict conflictEmptyResult => ApiResponse.Fail("资源冲突", "CONFLICT"),
            UnprocessableEntity<object> unprocessableResult => ApiResponse<object>.Fail("无法处理的实体",
                "UNPROCESSABLE_ENTITY", unprocessableResult.Value),
            UnprocessableEntity unprocessableEmptyResult => ApiResponse.Fail("无法处理的实体", "UNPROCESSABLE_ENTITY"),

            // 对于其他类型的结果，尝试提取状态码和内容
            _ => await HandleGenericResult(httpResult, context)
        };
    }

    private async Task<object> HandleGenericResult(IResult httpResult, EndpointFilterInvocationContext context)
    {
        // 创建一个临时的HttpContext来执行结果
        var httpContext = context.HttpContext;
        var originalResponse = httpContext.Response;

        // 创建内存流来捕获响应内容
        using var memoryStream = new MemoryStream();
        var tempResponse = new DefaultHttpContext().Response;
        tempResponse.Body = memoryStream;

        // 临时替换Response
        var originalBody = originalResponse.Body;
        originalResponse.Body = memoryStream;

        try
        {
            await httpResult.ExecuteAsync(httpContext);

            // 根据状态码决定如何包装响应
            var statusCode = originalResponse.StatusCode;

            // 重置流位置以读取内容
            memoryStream.Position = 0;
            var content = await new StreamReader(memoryStream).ReadToEndAsync();

            return statusCode switch
            {
                >= 200 and < 300 => string.IsNullOrEmpty(content)
                    ? ApiResponse.Ok()
                    : ApiResponse<object>.Ok(TryParseJson(content)),
                400 => ApiResponse.Fail("请求参数错误", "BAD_REQUEST"),
                401 => ApiResponse.Fail("未授权访问", "UNAUTHORIZED"),
                403 => ApiResponse.Fail("禁止访问", "FORBIDDEN"),
                404 => ApiResponse.Fail("资源未找到", "NOT_FOUND"),
                409 => ApiResponse.Fail("资源冲突", "CONFLICT"),
                422 => ApiResponse.Fail("无法处理的实体", "UNPROCESSABLE_ENTITY"),
                >= 500 => ApiResponse.Fail("服务器内部错误", "INTERNAL_ERROR"),
                _ => ApiResponse.Fail($"未知错误 (状态码: {statusCode})", "UNKNOWN_ERROR")
            };
        }
        finally
        {
            // 恢复原始Response
            originalResponse.Body = originalBody;
        }
    }

    private static object? TryParseJson(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        try
        {
            return JsonSerializer.Deserialize<object>(content);
        }
        catch
        {
            return content;
        }
    }
}