using ClaudeCodeProxy.Core;
using ClaudeCodeProxy.Domain;
using ClaudeCodeProxy.Host.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using ClaudeCodeProxy.Abstraction;

namespace ClaudeCodeProxy.Host.Services;

/// <summary>
/// 请求日志服务
/// </summary>
public class RequestLogService(IContext context, WalletService walletService)
{
    /// <summary>
    /// 创建请求日志
    /// </summary>
    public async Task<RequestLog> CreateRequestLogAsync(
        Guid userId,
        Guid apiKeyId,
        string apiKeyName,
        string model,
        DateTime requestStartTime,
        string platform = "claude",
        string? clientIp = null,
        string? userAgent = null,
        string? requestId = null,
        string? accountId = null,
        string? accountName = null,
        bool isStreaming = false,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var requestLog = new RequestLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ApiKeyId = apiKeyId,
            ApiKeyName = apiKeyName,
            AccountId = accountId,
            AccountName = accountName,
            Model = model,
            RequestStartTime = requestStartTime,
            Status = "pending",
            Platform = platform,
            ClientIp = clientIp,
            UserAgent = userAgent,
            RequestId = requestId,
            IsStreaming = isStreaming,
            Metadata = metadata != null ? JsonSerializer.Serialize(metadata, ThorJsonSerializer.DefaultOptions) : null,
            CreatedAt = DateTime.Now
        };

        requestLog.InitializeTimeFields();

        context.RequestLogs.Add(requestLog);
        await context.SaveAsync(cancellationToken);

        return requestLog;
    }

    /// <summary>
    /// 完成请求日志记录
    /// </summary>
    public async Task CompleteRequestLogAsync(
        Guid requestLogId,
        string status = "success",
        string? errorMessage = null,
        int? httpStatusCode = null,
        int inputTokens = 0,
        int outputTokens = 0,
        int cacheCreateTokens = 0,
        int cacheReadTokens = 0,
        decimal cost = 0,
        DateTime? requestEndTime = null,
        CancellationToken cancellationToken = default)
    {
        var requestLog = await context.RequestLogs
            .FirstOrDefaultAsync(r => r.Id == requestLogId, cancellationToken);

        if (requestLog == null)
        {
            return;
        }

        var endTime = requestEndTime ?? DateTime.Now;
        requestLog.CompleteRequest(endTime, status, errorMessage);
        requestLog.HttpStatusCode = httpStatusCode;
        requestLog.InputTokens = inputTokens;
        requestLog.OutputTokens = outputTokens;
        requestLog.CacheCreateTokens = cacheCreateTokens;
        requestLog.CacheReadTokens = cacheReadTokens;
        requestLog.Cost = cost;
        requestLog.CalculateTotalTokens();

        // 如果有费用且请求成功，从用户钱包扣费
        if (cost > 0 && status == "success")
        {
            var deductSuccess = await walletService.DeductWalletAsync(
                requestLog.UserId,
                cost,
                $"API调用费用 - {requestLog.Model}",
                requestLogId);

            if (!deductSuccess)
            {
                // 如果扣费失败，更新请求状态
                requestLog.Status = "failed";
                requestLog.ErrorMessage = "钱包余额不足";
            }
        }

        await context.SaveAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户请求日志
    /// </summary>
    public async Task<List<RequestLogDto>> GetUserRequestLogsAsync(
        Guid userId,
        int pageIndex = 0,
        int pageSize = 20,
        string? status = null,
        string? model = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.RequestLogs
            .Where(r => r.UserId == userId);

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(r => r.Status == status);
        }

        if (!string.IsNullOrEmpty(model))
        {
            query = query.Where(r => r.Model == model);
        }

        if (startDate.HasValue)
        {
            query = query.Where(r => r.RequestStartTime >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(r => r.RequestStartTime <= endDate.Value);
        }

        return await query
            .OrderByDescending(r => r.RequestStartTime)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .Select(r => new RequestLogDto
            {
                Id = r.Id,
                UserId = r.UserId,
                ApiKeyId = r.ApiKeyId,
                ApiKeyName = r.ApiKeyName,
                AccountId = r.AccountId,
                AccountName = r.AccountName,
                Model = r.Model,
                RequestStartTime = r.RequestStartTime,
                RequestEndTime = r.RequestEndTime,
                DurationMs = r.DurationMs,
                Status = r.Status,
                ErrorMessage = r.ErrorMessage,
                HttpStatusCode = r.HttpStatusCode,
                InputTokens = r.InputTokens,
                OutputTokens = r.OutputTokens,
                CacheCreateTokens = r.CacheCreateTokens,
                CacheReadTokens = r.CacheReadTokens,
                TotalTokens = r.TotalTokens,
                Cost = r.Cost,
                ClientIp = r.ClientIp,
                UserAgent = r.UserAgent,
                RequestId = r.RequestId,
                IsStreaming = r.IsStreaming,
                Platform = r.Platform,
                Metadata = r.Metadata,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户请求统计
    /// </summary>
    public async Task<UserRequestStatisticsDto> GetUserRequestStatisticsAsync(
        Guid userId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var start = startDate ?? DateTime.Now.AddDays(-30);
        var end = endDate ?? DateTime.Now;

        var logs = await context.RequestLogs
            .Where(r => r.UserId == userId && r.RequestStartTime >= start && r.RequestStartTime <= end)
            .ToListAsync(cancellationToken);

        var totalRequests = logs.Count;
        var successfulRequests = logs.Count(r => r.Status == "success");
        var failedRequests = totalRequests - successfulRequests;
        var totalTokens = logs.Sum(r => r.TotalTokens);
        var totalCost = logs.Sum(r => r.Cost);
        var averageResponseTime = logs.Any(r => r.DurationMs.HasValue)
            ? logs.Where(r => r.DurationMs.HasValue).Average(r => r.DurationMs ?? 0)
            : 0;
        var modelUsage = logs
            .GroupBy(r => r.Model)
            .Select(g => new ModelUsageDto
            {
                Model = g.Key,
                RequestCount = g.Count(),
                TotalTokens = g.Sum(r => r.TotalTokens),
                TotalCost = g.Sum(r => r.Cost)
            })
            .OrderByDescending(m => m.RequestCount)
            .ToList();

        var dailyUsage = logs
            .GroupBy(r => r.RequestDate)
            .Select(g => new DailyUsageDto
            {
                Date = g.Key,
                RequestCount = g.Count(),
                TotalTokens = g.Sum(r => r.TotalTokens),
                TotalCost = g.Sum(r => r.Cost),
                SuccessfulRequests = g.Count(r => r.Status == "success"),
                FailedRequests = g.Count(r => r.Status != "success")
            })
            .OrderBy(d => d.Date)
            .ToList();

        return new UserRequestStatisticsDto
        {
            UserId = userId,
            StartDate = start,
            EndDate = end,
            TotalRequests = totalRequests,
            SuccessfulRequests = successfulRequests,
            FailedRequests = failedRequests,
            SuccessRate = totalRequests > 0 ? (double)successfulRequests / totalRequests * 100 : 0,
            TotalTokens = totalTokens,
            TotalCost = totalCost,
            AverageResponseTime = averageResponseTime,
            ModelUsage = modelUsage,
            DailyUsage = dailyUsage
        };
    }

    /// <summary>
    /// 获取所有用户请求日志（管理员功能）
    /// </summary>
    public async Task<List<RequestLogDto>> GetAllRequestLogsAsync(
        int pageIndex = 0,
        int pageSize = 20,
        string? status = null,
        string? model = null,
        Guid? userId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.RequestLogs
            .Include(r => r.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(r => r.Status == status);
        }

        if (!string.IsNullOrEmpty(model))
        {
            query = query.Where(r => r.Model == model);
        }

        if (userId.HasValue)
        {
            query = query.Where(r => r.UserId == userId.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(r => r.RequestStartTime >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(r => r.RequestStartTime <= endDate.Value);
        }

        return await query
            .OrderByDescending(r => r.RequestStartTime)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .Select(r => new RequestLogDto
            {
                Id = r.Id,
                UserId = r.UserId,
                UserName = r.User.Username,
                ApiKeyId = r.ApiKeyId,
                ApiKeyName = r.ApiKeyName,
                AccountId = r.AccountId,
                AccountName = r.AccountName,
                Model = r.Model,
                RequestStartTime = r.RequestStartTime,
                RequestEndTime = r.RequestEndTime,
                DurationMs = r.DurationMs,
                Status = r.Status,
                ErrorMessage = r.ErrorMessage,
                HttpStatusCode = r.HttpStatusCode,
                InputTokens = r.InputTokens,
                OutputTokens = r.OutputTokens,
                CacheCreateTokens = r.CacheCreateTokens,
                CacheReadTokens = r.CacheReadTokens,
                TotalTokens = r.TotalTokens,
                Cost = r.Cost,
                ClientIp = r.ClientIp,
                UserAgent = r.UserAgent,
                RequestId = r.RequestId,
                IsStreaming = r.IsStreaming,
                Platform = r.Platform,
                Metadata = r.Metadata,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
}