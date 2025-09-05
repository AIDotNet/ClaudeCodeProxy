using ClaudeCodeProxy.Domain;

namespace ClaudeCodeProxy.Host.Models;

/// <summary>
///     生成OpenAI OAuth授权URL请求模型
/// </summary>
public record GenerateOpenAiOAuthUrlRequest(
    string? ClientId = null,
    string? RedirectUri = null,
    ProxyConfig? Proxy = null
);

/// <summary>
///     处理OpenAI OAuth授权码请求模型
/// </summary>
public record ExchangeOpenAiOAuthCodeRequest(
    string AuthorizationCode,
    string SessionId, // 用于从缓存中获取OAuth会话数据
    string? AccountName = null,
    string? Description = null,
    string? AccountType = null,
    int? Priority = null,
    ProxyConfig? Proxy = null
);