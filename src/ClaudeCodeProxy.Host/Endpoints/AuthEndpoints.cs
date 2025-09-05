using ClaudeCodeProxy.Host.Filters;
using ClaudeCodeProxy.Host.Models;
using ClaudeCodeProxy.Host.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ClaudeCodeProxy.Host.Endpoints;

/// <summary>
///     认证相关端点
/// </summary>
public static class AuthEndpoints
{
    /// <summary>
    ///     配置认证相关端点
    /// </summary>
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .AddEndpointFilter<GlobalResponseFilter>()
            .WithTags("Authentication")
            .WithOpenApi();

        group.MapPost("/login", Login)
            .WithName("Login")
            .WithSummary("用户登录")
            .WithDescription("使用用户名和密码进行登录，返回JWT令牌")
            .AllowAnonymous();

        group.MapPost("/register", Register)
            .WithName("Register")
            .WithSummary("用户注册")
            .WithDescription("注册新用户，可以使用邀请码")
            .AllowAnonymous();
    }

    /// <summary>
    ///     用户登录
    /// </summary>
    private static async Task<Results<Ok<LoginResponse>, UnauthorizedHttpResult, BadRequest<string>>> Login(
        LoginRequest request,
        AuthService authService,
        HttpContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return TypedResults.BadRequest("用户名和密码不能为空");

        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        var userAgent = context.Request.Headers.UserAgent.ToString();

        var loginResponse = await authService.LoginAsync(request, ipAddress, userAgent);
        if (loginResponse == null) return TypedResults.Unauthorized();

        return TypedResults.Ok(loginResponse);
    }

    /// <summary>
    ///     用户注册
    /// </summary>
    private static async Task<Results<Ok<LoginResponse>, BadRequest<string>>> Register(
        RegisterUserRequest request,
        AuthService authService,
        HttpContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return TypedResults.BadRequest("用户名和密码不能为空");

        if (string.IsNullOrWhiteSpace(request.Email)) return TypedResults.BadRequest("邮箱不能为空");

        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        var userAgent = context.Request.Headers.UserAgent.ToString();

        var registerResponse = await authService.RegisterAsync(request, ipAddress, userAgent);
        if (registerResponse == null) return TypedResults.BadRequest("注册失败");

        return TypedResults.Ok(registerResponse);
    }
}