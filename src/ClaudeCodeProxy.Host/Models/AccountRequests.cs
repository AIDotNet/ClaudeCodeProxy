using ClaudeCodeProxy.Domain;
using ClaudeCodeProxy.Host.Helper;

namespace ClaudeCodeProxy.Host.Models;

/// <summary>
/// 创建账户请求模型
/// </summary>
public record CreateAccountRequest(
    string Name,
    string Description,
    string ApiKey,
    string ApiUrl,
    string AccountType,
    string? UserAgent = null,
    ProxyConfig? Proxy = null,
    ClaudeAiOauth? ClaudeAiOauth = null,
    OpenAiOauth? OpenAiOauth = null,
    int Priority = 0
);

/// <summary>
/// 更新账户请求模型
/// </summary>
public record UpdateAccountRequest(
    string? Name = null,
    string? Description = null,
    string? AccountType = null,
    int? Priority = null,
    string? ProjectId = null,
    string? ApiUrl = null,
    string? ApiKey = null,
    string? UserAgent = null,
    int? RateLimitDuration = null,
    Dictionary<string, string>? SupportedModels = null,
    ClaudeAiOauth? ClaudeAiOauth = null,
    OpenAiOauth? OpenAiOauth = null,
    object? GeminiOauth = null,
    ProxyConfig? Proxy = null,
    bool? IsActive = null
);