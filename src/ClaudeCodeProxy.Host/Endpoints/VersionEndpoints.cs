using ClaudeCodeProxy.Host.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ClaudeCodeProxy.Host.Endpoints;

/// <summary>
///     版本管理相关端点
/// </summary>
public static class VersionEndpoints
{
    /// <summary>
    ///     配置版本管理相关端点
    /// </summary>
    public static void MapVersionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/version")
            .WithTags("Version")
            .WithOpenApi();

        // 获取当前版本
        group.MapGet("/current", GetCurrentVersion)
            .WithName("GetCurrentVersion")
            .WithSummary("获取当前应用版本")
            .WithDescription("获取当前运行的应用程序版本号")
            .Produces<VersionInfo>();

        // 检查更新
        group.MapGet("/check-updates", CheckForUpdates)
            .WithName("CheckForUpdates")
            .WithSummary("检查版本更新")
            .WithDescription("检查是否有新版本可用，对比当前版本和GitHub最新Release")
            .Produces<VersionCheckResult>();

        // 获取最新版本信息
        group.MapGet("/latest", GetLatestVersion)
            .WithName("GetLatestVersion")
            .WithSummary("获取最新版本信息")
            .WithDescription("从GitHub获取最新Release版本信息")
            .Produces<GitHubReleaseInfo>();
    }

    /// <summary>
    ///     获取当前版本
    /// </summary>
    private static Ok<VersionInfo> GetCurrentVersion(
        [FromServices] VersionService versionService)
    {
        var currentVersion = versionService.GetCurrentVersion();

        return TypedResults.Ok(new VersionInfo
        {
            Version = currentVersion,
            Timestamp = DateTime.Now
        });
    }

    /// <summary>
    ///     检查更新
    /// </summary>
    private static async Task<Results<Ok<VersionCheckResult>, BadRequest<string>>> CheckForUpdates(
        [FromServices] VersionService versionService,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await versionService.CheckForUpdatesAsync(cancellationToken);
            return TypedResults.Ok(result);
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest($"检查更新失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     获取最新版本信息
    /// </summary>
    private static async Task<Results<Ok<GitHubReleaseInfo>, NotFound<string>, BadRequest<string>>> GetLatestVersion(
        [FromServices] VersionService versionService,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var latestRelease = await versionService.GetLatestVersionAsync(cancellationToken);

            if (latestRelease == null) return TypedResults.NotFound("无法获取最新版本信息");

            return TypedResults.Ok(latestRelease);
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest($"获取最新版本失败: {ex.Message}");
        }
    }
}

/// <summary>
///     版本信息
/// </summary>
public class VersionInfo
{
    /// <summary>
    ///     版本号
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    ///     时间戳
    /// </summary>
    public DateTime Timestamp { get; set; }
}