namespace ClaudeCodeProxy.Host.Models;

/// <summary>
/// 创建API Key请求模型
/// </summary>
public record CreateApiKeyRequest(
    string Name,
    string KeyValue,
    Guid? UserId = null, // 如果为空，则由当前登录用户的ID填充
    string? Description = null,
    List<string>? Tags = null,
    int? TokenLimit = null,
    int? RateLimitWindow = null,
    int? RateLimitRequests = null,
    int ConcurrencyLimit = 0,
    decimal DailyCostLimit = 0,
    decimal MonthlyCostLimit = 0,
    decimal TotalCostLimit = 0,
    DateTime? ExpiresAt = null,
    string Permissions = "all",
    string? ClaudeAccountId = null,
    string? ClaudeConsoleAccountId = null,
    string? GeminiAccountId = null,
    bool EnableModelRestriction = false,
    List<string>? RestrictedModels = null,
    bool EnableClientRestriction = false,
    List<string>? AllowedClients = null,
    bool IsEnabled = true,
    string? Model = null,
    string Service = "claude",
    string? DefaultAccountId = null
);