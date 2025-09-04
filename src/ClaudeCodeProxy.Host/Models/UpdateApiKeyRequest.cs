namespace ClaudeCodeProxy.Host.Models;

/// <summary>
/// 更新API Key请求模型
/// </summary>
public record UpdateApiKeyRequest(
    string? Model = null,
    string? Name = null,
    string? Description = null,
    DateTime? ExpiresAt = null,
    bool? IsEnabled = null,
    decimal? DailyCostLimit = null,
    decimal? MonthlyCostLimit = null,
    decimal? TotalCostLimit = null,
    List<string>? Tags = null,
    int? TokenLimit = null,
    int? RateLimitWindow = null,
    int? RateLimitRequests = null,
    int? ConcurrencyLimit = null,
    string? Permissions = null,
    string? ClaudeAccountId = null,
    string? ClaudeConsoleAccountId = null,
    string? GeminiAccountId = null,
    bool? EnableModelRestriction = null,
    List<string>? RestrictedModels = null,
    bool? EnableClientRestriction = null,
    List<string>? AllowedClients = null,
    string? Service = null,
    string? DefaultAccountId = null
);