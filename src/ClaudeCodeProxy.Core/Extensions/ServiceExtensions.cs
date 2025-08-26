using ClaudeCodeProxy.Abstraction.Chats;
using ClaudeCodeProxy.Core.AI;
using ClaudeCodeProxy.Core.AI.Responses;
using Microsoft.Extensions.DependencyInjection;
using Thor.Abstractions.Chats;
using Thor.Abstractions.Responses;

namespace ClaudeCodeProxy.Core.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        // 添加核心服务
        services.AddScoped<IAnthropicChatCompletionsService, AnthropicChatService>();
        services.AddScoped<IThorChatCompletionsService, OpenAIChatCompletionsService>();
        services.AddScoped<IThorResponsesService, OpenAIResponsesService>();
        services.AddScoped<OpenAIAnthropicChatCompletionsService>();
        services.AddScoped<OpenAiAnthropicResponsesService>();
        
        
        return services;
    }
}