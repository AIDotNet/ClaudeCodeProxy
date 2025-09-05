using System.Security;

namespace ClaudeCodeProxy.Host.Middlewares;

public class GlobalMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception e)
        {
            await HandleExceptionAsync(context, e);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // 如果响应已经开始，不能再设置状态码
        if (context.Response.HasStarted) return;

        context.Response.ContentType = "application/json";

        var (statusCode, message) = GetErrorResponse(exception);
        context.Response.StatusCode = statusCode;

        var errorResponse = new
        {
            message,
            success = false,
            error = new
            {
                type = exception.GetType().Name,
                code = statusCode.ToString()
            }
        };

        await context.Response.WriteAsJsonAsync(errorResponse);
    }

    private static (int statusCode, string message) GetErrorResponse(Exception exception)
    {
        return exception switch
        {
            // JWT/认证相关异常
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "访问令牌已过期或无效，请重新登录"),
            SecurityException => (StatusCodes.Status401Unauthorized, "安全验证失败，请重新登录"),

            // 检查异常消息中的关键词来判断是否为JWT相关异常
            _ when IsJwtRelatedError(exception) => (StatusCodes.Status401Unauthorized, "身份验证已过期，请重新登录"),

            // 其他业务异常
            ArgumentException => (StatusCodes.Status400BadRequest, exception.Message),
            InvalidOperationException => (StatusCodes.Status400BadRequest, exception.Message),

            // 默认500错误
            _ => (StatusCodes.Status500InternalServerError, exception.Message)
        };
    }

    private static bool IsJwtRelatedError(Exception exception)
    {
        var message = exception.Message.ToLowerInvariant();
        var type = exception.GetType().Name.ToLowerInvariant();

        var jwtKeywords = new[]
        {
            "token", "jwt", "bearer", "expired", "invalid", "unauthorized",
            "authentication", "authorization", "signature", "claim"
        };

        return jwtKeywords.Any(keyword => message.Contains(keyword) || type.Contains(keyword));
    }
}