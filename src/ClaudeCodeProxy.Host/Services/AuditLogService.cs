using System.Text.Json;
using ClaudeCodeProxy.Core;
using ClaudeCodeProxy.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClaudeCodeProxy.Host.Services;

/// <summary>
///     审计日志服务
/// </summary>
public class AuditLogService(
    IContext context,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuditLogService> logger)
{
    /// <summary>
    ///     记录用户账户绑定操作日志
    /// </summary>
    public async Task LogUserAccountBindingAsync(
        Guid userId,
        string action,
        string accountId,
        object? oldValues = null,
        object? newValues = null,
        string result = "success",
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        await LogAsync(
            userId,
            action,
            "UserAccountBinding",
            accountId,
            $"用户 {userId} 对账户 {accountId} 执行 {action} 操作",
            oldValues,
            newValues,
            result,
            errorMessage,
            cancellationToken);
    }

    /// <summary>
    ///     记录API Key账户绑定操作日志
    /// </summary>
    public async Task LogApiKeyAccountBindingAsync(
        Guid userId,
        string action,
        Guid apiKeyId,
        object? oldValues = null,
        object? newValues = null,
        string result = "success",
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        await LogAsync(
            userId,
            action,
            "ApiKeyAccountBinding",
            apiKeyId.ToString(),
            $"用户 {userId} 对API Key {apiKeyId} 执行 {action} 操作",
            oldValues,
            newValues,
            result,
            errorMessage,
            cancellationToken);
    }

    /// <summary>
    ///     记录账户访问日志
    /// </summary>
    public async Task LogAccountAccessAsync(
        Guid userId,
        string action,
        string accountId,
        string? details = null,
        string result = "success",
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        await LogAsync(
            userId,
            action,
            "Account",
            accountId,
            details ?? $"用户 {userId} 访问账户 {accountId}",
            null,
            null,
            result,
            errorMessage,
            cancellationToken);
    }

    /// <summary>
    ///     记录通用审计日志
    /// </summary>
    public async Task LogAsync(
        Guid userId,
        string action,
        string resourceType,
        string? resourceId = null,
        string? details = null,
        object? oldValues = null,
        object? newValues = null,
        string result = "success",
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var httpContext = httpContextAccessor.HttpContext;
            var ipAddress = GetClientIpAddress(httpContext);
            var userAgent = httpContext?.Request.Headers.UserAgent.ToString();

            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Action = action,
                ResourceType = resourceType,
                ResourceId = resourceId,
                Details = details,
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues, JsonSerializerOptions.Web) : null,
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues, JsonSerializerOptions.Web) : null,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Result = result,
                ErrorMessage = errorMessage,
                CreatedAt = DateTime.Now
            };

            context.AuditLogs.Add(auditLog);
            await context.SaveAsync(cancellationToken);

            logger.LogInformation(
                "审计日志已记录: 用户 {UserId} 执行 {Action} 操作，资源类型: {ResourceType}，资源ID: {ResourceId}，结果: {Result}",
                userId, action, resourceType, resourceId, result);
        }
        catch (Exception ex)
        {
            // 审计日志记录失败不应影响主业务逻辑
            logger.LogError(ex, "记录审计日志时发生异常: 用户 {UserId}，操作 {Action}，资源 {ResourceType}:{ResourceId}",
                userId, action, resourceType, resourceId);
        }
    }

    /// <summary>
    ///     获取客户端IP地址
    /// </summary>
    private static string? GetClientIpAddress(HttpContext? httpContext)
    {
        if (httpContext == null) return null;

        // 检查X-Forwarded-For头（如果使用代理）
        var xForwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xForwardedFor))
            // X-Forwarded-For可能包含多个IP，取第一个
            return xForwardedFor.Split(',')[0].Trim();

        // 检查X-Real-IP头
        var xRealIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xRealIp)) return xRealIp;

        // 使用RemoteIpAddress
        return httpContext.Connection.RemoteIpAddress?.ToString();
    }

    /// <summary>
    ///     查询审计日志
    /// </summary>
    public async Task<List<AuditLog>> GetAuditLogsAsync(
        Guid? userId = null,
        string? action = null,
        string? resourceType = null,
        string? resourceId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var query = context.AuditLogs.AsQueryable();

        if (userId.HasValue) query = query.Where(x => x.UserId == userId.Value);

        if (!string.IsNullOrEmpty(action)) query = query.Where(x => x.Action == action);

        if (!string.IsNullOrEmpty(resourceType)) query = query.Where(x => x.ResourceType == resourceType);

        if (!string.IsNullOrEmpty(resourceId)) query = query.Where(x => x.ResourceId == resourceId);

        if (startDate.HasValue) query = query.Where(x => x.CreatedAt >= startDate.Value);

        if (endDate.HasValue) query = query.Where(x => x.CreatedAt <= endDate.Value);

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}