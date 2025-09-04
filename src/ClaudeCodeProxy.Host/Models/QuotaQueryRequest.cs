namespace ClaudeCodeProxy.Host.Models;

/// <summary>
/// 额度查询请求模型
/// </summary>
public record QuotaQueryRequest(
    string ApiKey
);