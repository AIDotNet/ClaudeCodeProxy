using ClaudeCodeProxy.Host.Models;
using ClaudeCodeProxy.Host.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ClaudeCodeProxy.Host.Endpoints;

/// <summary>
///     Claude代理相关端点
/// </summary>
public static class ClaudeProxyEndpoints
{
    /// <summary>
    ///     配置Claude代理相关路由
    /// </summary>
    public static void MapClaudeProxyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/claude-proxy")
            .WithTags("ClaudeProxy")
            .WithOpenApi();

        // 生成OAuth授权URL
        group.MapPost("/auth/generate-url", GenerateAuthUrl)
            .WithName("GenerateAuthUrl")
            .WithSummary("生成OAuth授权URL")
            .Produces<GenerateAuthUrlResult>()
            .Produces(400);

        // 交换授权码获取访问令牌
        group.MapPost("/auth/exchange-code", ExchangeCode)
            .WithName("ExchangeCode")
            .WithSummary("交换授权码获取访问令牌")
            .Produces<object>()
            .Produces(400);
    }

    /// <summary>
    ///     生成OAuth授权URL
    /// </summary>
    private static async Task<Results<Ok<GenerateAuthUrlResult>, BadRequest<string>>> GenerateAuthUrl(
        GenerateAuthUrlInput request,
        ClaudeProxyService claudeProxyService)
    {
        try
        {
            var result = claudeProxyService.GenerateAuthUrl(request);
            return TypedResults.Ok(result);
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest($"生成授权URL失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     交换授权码获取访问令牌
    /// </summary>
    private static async Task<Results<Ok<object>, BadRequest<string>>> ExchangeCode(
        ExchangeCodeInput request,
        ClaudeProxyService claudeProxyService)
    {
        try
        {
            var result = await claudeProxyService.ExchangeCode(request);
            return TypedResults.Ok(result);
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest($"参数错误: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest($"操作无效: {ex.Message}");
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest($"交换授权码失败: {ex.Message}");
        }
    }
}