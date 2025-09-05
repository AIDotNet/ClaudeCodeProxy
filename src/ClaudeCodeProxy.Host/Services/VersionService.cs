using System.Reflection;
using System.Text.Json;

namespace ClaudeCodeProxy.Host.Services;

/// <summary>
///     版本管理服务
/// </summary>
public class VersionService
{
    private const string GitHubApiUrl = "https://api.github.com/repos/AIDotNet/ClaudeCodeProxy/releases/latest";
    private readonly HttpClient _httpClient;
    private readonly ILogger<VersionService> _logger;

    public VersionService(HttpClient httpClient, ILogger<VersionService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // 设置GitHub API请求头
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "ClaudeCodeProxy");
    }

    /// <summary>
    ///     获取当前应用版本
    /// </summary>
    public string GetCurrentVersion()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                          ?? assembly.GetName().Version?.ToString()
                          ?? "0.1.5";

            return version;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取当前版本失败，使用默认版本");
            return "0.1.5";
        }
    }

    /// <summary>
    ///     从GitHub获取最新版本信息
    /// </summary>
    public async Task<GitHubReleaseInfo?> GetLatestVersionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(GitHubApiUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GitHub API请求失败: {StatusCode}", response.StatusCode);
                return null;
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };

            var releaseInfo = JsonSerializer.Deserialize<GitHubReleaseInfo>(jsonContent, options);
            return releaseInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取GitHub最新版本信息失败");
            return null;
        }
    }

    /// <summary>
    ///     检查是否有新版本可用
    /// </summary>
    public async Task<VersionCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var currentVersion = GetCurrentVersion();
        var latestRelease = await GetLatestVersionAsync(cancellationToken);

        if (latestRelease == null)
            return new VersionCheckResult
            {
                CurrentVersion = currentVersion,
                HasUpdate = false,
                ErrorMessage = "无法获取最新版本信息"
            };

        var latestVersion = latestRelease.TagName?.TrimStart('v') ?? latestRelease.Name ?? "";
        var hasUpdate = CompareVersions(currentVersion, latestVersion) < 0;

        return new VersionCheckResult
        {
            CurrentVersion = currentVersion,
            LatestVersion = latestVersion,
            HasUpdate = hasUpdate,
            ReleaseInfo = latestRelease
        };
    }

    /// <summary>
    ///     比较版本号
    /// </summary>
    /// <param name="current">当前版本</param>
    /// <param name="latest">最新版本</param>
    /// <returns>-1: current < latest, 0 : current== latest, 1 : current> latest</returns>
    private static int CompareVersions(string current, string latest)
    {
        try
        {
            // 移除可能的前缀（如 'v'）
            current = current.TrimStart('v');
            latest = latest.TrimStart('v');

            var currentVersion = new Version(current);
            var latestVersion = new Version(latest);

            return currentVersion.CompareTo(latestVersion);
        }
        catch
        {
            // 如果版本号格式不标准，使用字符串比较
            return string.Compare(current, latest, StringComparison.OrdinalIgnoreCase);
        }
    }
}

/// <summary>
///     GitHub Release信息
/// </summary>
public class GitHubReleaseInfo
{
    public string? TagName { get; set; }
    public string? Name { get; set; }
    public string? Body { get; set; }
    public string? HtmlUrl { get; set; }
    public DateTime? PublishedAt { get; set; }
    public bool? Prerelease { get; set; }
    public bool? Draft { get; set; }
}

/// <summary>
///     版本检查结果
/// </summary>
public class VersionCheckResult
{
    /// <summary>
    ///     当前版本
    /// </summary>
    public string CurrentVersion { get; set; } = string.Empty;

    /// <summary>
    ///     最新版本
    /// </summary>
    public string? LatestVersion { get; set; }

    /// <summary>
    ///     是否有更新
    /// </summary>
    public bool HasUpdate { get; set; }

    /// <summary>
    ///     错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    ///     Release信息
    /// </summary>
    public GitHubReleaseInfo? ReleaseInfo { get; set; }
}