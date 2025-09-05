using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;

namespace ClaudeCodeProxy.Host.Services;

/// <summary>
///     OAuth第三方登录服务
/// </summary>
public class OAuthService(IHttpClientFactory httpClientFactory, UserService userService, IConfiguration configuration)
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();

    /// <summary>
    ///     生成GitHub授权URL
    /// </summary>
    public string GenerateGitHubAuthUrl(string redirectUri, string? state = null)
    {
        var config = configuration.GetSection("OAuth:GitHub").Get<GitHubConfig>();
        if (config == null || string.IsNullOrEmpty(config.ClientId))
            throw new InvalidOperationException("GitHub OAuth配置未找到");

        var scope = "user:email";
        var url = "https://github.com/login/oauth/authorize" +
                  $"?client_id={Uri.EscapeDataString(config.ClientId)}" +
                  $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                  $"&scope={Uri.EscapeDataString(scope)}";

        if (!string.IsNullOrEmpty(state)) url += $"&state={Uri.EscapeDataString(state)}";

        return url;
    }

    /// <summary>
    ///     生成Gitee授权URL
    /// </summary>
    public string GenerateGiteeAuthUrl(string redirectUri, string? state = null)
    {
        var config = configuration.GetSection("OAuth:Gitee").Get<GiteeConfig>();
        if (config == null || string.IsNullOrEmpty(config.ClientId))
            throw new InvalidOperationException("Gitee OAuth配置未找到");

        var scope = "user_info emails";
        var url = "https://gitee.com/oauth/authorize" +
                  $"?client_id={Uri.EscapeDataString(config.ClientId)}" +
                  $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                  "&response_type=code" +
                  $"&scope={Uri.EscapeDataString(scope)}";

        if (!string.IsNullOrEmpty(state)) url += $"&state={Uri.EscapeDataString(state)}";

        return url;
    }

    /// <summary>
    ///     生成Google授权URL
    /// </summary>
    public string GenerateGoogleAuthUrl(string redirectUri, string? state = null)
    {
        var config = configuration.GetSection("OAuth:Google").Get<GoogleConfig>();
        if (config == null || string.IsNullOrEmpty(config.ClientId))
            throw new InvalidOperationException("Google OAuth配置未找到");

        var scope = "openid email profile";
        var url = "https://accounts.google.com/o/oauth2/v2/auth" +
                  $"?client_id={Uri.EscapeDataString(config.ClientId)}" +
                  $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                  "&response_type=code" +
                  $"&scope={Uri.EscapeDataString(scope)}";

        if (!string.IsNullOrEmpty(state)) url += $"&state={Uri.EscapeDataString(state)}";

        return url;
    }

    /// <summary>
    ///     处理GitHub回调
    /// </summary>
    public async Task<OAuthUserInfo?> HandleGitHubCallbackAsync(string code, string redirectUri)
    {
        try
        {
            var config = configuration.GetSection("OAuth:GitHub").Get<GitHubConfig>();
            if (config == null) throw new InvalidOperationException("GitHub OAuth配置未找到");

            // 获取访问令牌
            var tokenResponse = await _httpClient.PostAsync("https://github.com/login/oauth/access_token",
                new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", config.ClientId),
                    new KeyValuePair<string, string>("client_secret", config.ClientSecret),
                    new KeyValuePair<string, string>("code", code),
                    new KeyValuePair<string, string>("redirect_uri", redirectUri)
                }));

            var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
            var tokenParams = HttpUtility.ParseQueryString(tokenContent);
            var accessToken = tokenParams["access_token"];

            if (string.IsNullOrEmpty(accessToken)) return null;

            // 获取用户信息
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            var userResponse = await _httpClient.GetAsync("https://api.github.com/user");
            var userJson = await userResponse.Content.ReadAsStringAsync();
            var userInfo = JsonSerializer.Deserialize<JsonElement>(userJson);

            // 获取用户邮箱
            var emailResponse = await _httpClient.GetAsync("https://api.github.com/user/emails");
            var emailJson = await emailResponse.Content.ReadAsStringAsync();
            var emails = JsonSerializer.Deserialize<JsonElement[]>(emailJson);
            var primaryEmail = emails?.FirstOrDefault(e =>
                e.GetProperty("primary").GetBoolean() &&
                e.GetProperty("verified").GetBoolean());

            return new OAuthUserInfo
            {
                Provider = "github",
                ProviderId = userInfo.GetProperty("id").GetInt32().ToString(),
                Email = primaryEmail?.GetProperty("email").GetString() ?? "",
                Name = userInfo.GetProperty("name").GetString(),
                Avatar = userInfo.GetProperty("avatar_url").GetString()
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    ///     处理Gitee回调
    /// </summary>
    public async Task<OAuthUserInfo?> HandleGiteeCallbackAsync(string code, string redirectUri)
    {
        try
        {
            var config = configuration.GetSection("OAuth:Gitee").Get<GiteeConfig>();
            if (config == null) throw new InvalidOperationException("Gitee OAuth配置未找到");

            // 获取访问令牌
            var tokenResponse = await _httpClient.PostAsync("https://gitee.com/oauth/token",
                new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("client_id", config.ClientId),
                    new KeyValuePair<string, string>("client_secret", config.ClientSecret),
                    new KeyValuePair<string, string>("code", code),
                    new KeyValuePair<string, string>("redirect_uri", redirectUri)
                }));

            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var tokenInfo = JsonSerializer.Deserialize<JsonElement>(tokenJson);
            var accessToken = tokenInfo.GetProperty("access_token").GetString();

            if (string.IsNullOrEmpty(accessToken)) return null;

            // 获取用户信息
            var userResponse = await _httpClient.GetAsync($"https://gitee.com/api/v5/user?access_token={accessToken}");
            var userJson = await userResponse.Content.ReadAsStringAsync();
            var userInfo = JsonSerializer.Deserialize<JsonElement>(userJson);

            return new OAuthUserInfo
            {
                Provider = "gitee",
                ProviderId = userInfo.GetProperty("id").GetInt32().ToString(),
                Email = userInfo.GetProperty("email").GetString() ?? "",
                Name = userInfo.GetProperty("name").GetString(),
                Avatar = userInfo.GetProperty("avatar_url").GetString()
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    ///     处理Google回调
    /// </summary>
    public async Task<OAuthUserInfo?> HandleGoogleCallbackAsync(string code, string redirectUri)
    {
        try
        {
            var config = configuration.GetSection("OAuth:Google").Get<GoogleConfig>();
            if (config == null) throw new InvalidOperationException("Google OAuth配置未找到");

            // 获取访问令牌
            var tokenResponse = await _httpClient.PostAsync("https://oauth2.googleapis.com/token",
                new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("client_id", config.ClientId),
                    new KeyValuePair<string, string>("client_secret", config.ClientSecret),
                    new KeyValuePair<string, string>("code", code),
                    new KeyValuePair<string, string>("redirect_uri", redirectUri)
                }));

            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var tokenInfo = JsonSerializer.Deserialize<JsonElement>(tokenJson);
            var accessToken = tokenInfo.GetProperty("access_token").GetString();

            if (string.IsNullOrEmpty(accessToken)) return null;

            // 获取用户信息
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            var userResponse = await _httpClient.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
            var userJson = await userResponse.Content.ReadAsStringAsync();
            var userInfo = JsonSerializer.Deserialize<JsonElement>(userJson);

            return new OAuthUserInfo
            {
                Provider = "google",
                ProviderId = userInfo.GetProperty("id").GetString() ?? "",
                Email = userInfo.GetProperty("email").GetString() ?? "",
                Name = userInfo.GetProperty("name").GetString(),
                Avatar = userInfo.GetProperty("picture").GetString()
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    ///     GitHub OAuth配置
    /// </summary>
    public class GitHubConfig
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
    }

    /// <summary>
    ///     Gitee OAuth配置
    /// </summary>
    public class GiteeConfig
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
    }

    /// <summary>
    ///     Google OAuth配置
    /// </summary>
    public class GoogleConfig
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
    }

    /// <summary>
    ///     OAuth用户信息
    /// </summary>
    public class OAuthUserInfo
    {
        public string Provider { get; set; } = string.Empty;
        public string ProviderId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Avatar { get; set; }
    }
}