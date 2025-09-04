using ClaudeCodeProxy.Host.Models;
using ClaudeCodeProxy.Host.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClaudeCodeProxy.Host.Endpoints;

/// <summary>
/// 额度查询端点
/// </summary>
public static class QuotaEndpoints
{
    public static void MapQuotaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/quota")
            .WithTags("Quota")
            .WithDescription("额度查询相关接口");

        // 查询API Key额度信息
        group.MapPost("/query", QueryQuotaAsync)
            .WithName("QueryQuota")
            .WithSummary("查询API Key额度信息")
            .WithDescription("根据API Key查询额度使用情况，不需要身份验证")
            .AllowAnonymous()
            .Produces<QuotaQueryResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// 查询API Key额度信息
    /// </summary>
    private static async Task<IResult> QueryQuotaAsync(
        [FromBody] QuotaQueryRequest request,
        [FromServices] ApiKeyService apiKeyService,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ApiKey))
            {
                return Results.BadRequest(new { error = "API Key不能为空" });
            }

            var quotaInfo = await apiKeyService.QueryQuotaAsync(request.ApiKey, cancellationToken);

            if (quotaInfo == null)
            {
                return Results.NotFound(new { error = "API Key不存在或已禁用" });
            }

            return Results.Ok(quotaInfo);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "查询额度信息失败",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }
}