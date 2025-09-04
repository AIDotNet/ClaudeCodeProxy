using ClaudeCodeProxy.Abstraction.Chats;
using ClaudeCodeProxy.Core;
using ClaudeCodeProxy.Host.Services;
using ClaudeCodeProxy.Host.Services.AI;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Thor.Abstractions;
using Thor.Abstractions.Anthropic;

namespace ClaudeCodeProxy.Host.Endpoints;

/// <summary>
/// 消息相关端点
/// </summary>
public static class MessageEndpoints
{
    /// <summary>
    /// 配置消息相关端点
    /// </summary>
    public static void MapMessageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/messages")
            .WithTags("Messages")
            .WithOpenApi();

        group.MapPost("/", (MessageService messageService,
                    HttpContext httpContext,
                    [FromServices] ApiKeyService keyService,
                    [FromServices] RequestLogService requestLogService,
                    [FromServices] WalletService walletService,
                    [FromBody] AnthropicInput request,
                    [FromServices] IContext context,
                    [FromServices] IAnthropicChatCompletionsService chatCompletionsService) =>
                messageService.HandleAsync(httpContext, keyService, requestLogService, walletService, request,context, chatCompletionsService))
            .WithName("HandleMessage")
            .WithSummary("处理消息请求")
            .WithDescription("处理Claude消息请求，支持流式和非流式响应");
    }
}