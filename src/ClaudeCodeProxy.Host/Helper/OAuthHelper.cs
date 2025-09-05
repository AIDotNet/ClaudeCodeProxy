using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using ClaudeCodeProxy.Domain;

namespace ClaudeCodeProxy.Host.Helper;

public static class OAuthConfig
{
    public const string AuthorizeUrl = "https://claude.ai/oauth/authorize";
    public const string TokenUrl = "https://console.anthropic.com/v1/oauth/token";
    public const string ClientId = "9d1c250a-e61b-44d9-88ed-5944d1962f5e";
    public const string RedirectUri = "https://console.anthropic.com/oauth/code/callback";
    public const string Scopes = "org:create_api_key user:profile user:inference";
}

public class OAuthParams
{
    public string AuthUrl { get; set; } = string.Empty;
    public string CodeVerifier { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string CodeChallenge { get; set; } = string.Empty;
}

public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public long ExpiresAt { get; set; }
    public string[] Scopes { get; set; } = Array.Empty<string>();
    public bool IsMax { get; set; } = true;
}

public class ClaudeCredentials
{
    public ClaudeAiOAuth ClaudeAiOauth { get; set; } = new();
}

public class ClaudeAiOAuth
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public long ExpiresAt { get; set; }
    public string[] Scopes { get; set; } = Array.Empty<string>();
    public bool IsMax { get; set; } = true;
}

public class OAuthHelper
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OAuthHelper> _logger;

    public OAuthHelper(ILogger<OAuthHelper> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public string GenerateState()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public string GenerateCodeVerifier()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public string GenerateCodeChallenge(string codeVerifier)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(codeVerifier);
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hashBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public string GenerateAuthUrl(string codeChallenge, string state)
    {
        var queryParams = HttpUtility.ParseQueryString(string.Empty);
        queryParams["code"] = "true";
        queryParams["client_id"] = OAuthConfig.ClientId;
        queryParams["response_type"] = "code";
        queryParams["redirect_uri"] = OAuthConfig.RedirectUri;
        queryParams["scope"] = OAuthConfig.Scopes;
        queryParams["code_challenge"] = codeChallenge;
        queryParams["code_challenge_method"] = "S256";
        queryParams["state"] = state;

        return $"{OAuthConfig.AuthorizeUrl}?{queryParams}";
    }

    public OAuthParams GenerateOAuthParams()
    {
        var state = GenerateState();
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);
        var authUrl = GenerateAuthUrl(codeChallenge, state);

        return new OAuthParams
        {
            AuthUrl = authUrl,
            CodeVerifier = codeVerifier,
            State = state,
            CodeChallenge = codeChallenge
        };
    }

    public async Task<TokenResponse> ExchangeCodeForTokensAsync(string authorizationCode, string codeVerifier,
        string state, ProxyConfig? proxyConfig = null)
    {
        var cleanedCode = authorizationCode.Split('#')[0]?.Split('&')[0] ?? authorizationCode;

        var parameters = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = OAuthConfig.ClientId,
            ["code"] = cleanedCode,
            ["redirect_uri"] = OAuthConfig.RedirectUri,
            ["code_verifier"] = codeVerifier,
            ["state"] = state
        };

        using var httpClient = CreateHttpClientWithProxy(proxyConfig);

        try
        {
            _logger.LogDebug("🔄 Attempting OAuth token exchange", new Dictionary<string, object>
            {
                ["url"] = OAuthConfig.TokenUrl,
                ["codeLength"] = cleanedCode.Length,
                ["codePrefix"] = cleanedCode.Length > 10 ? cleanedCode[..10] + "..." : cleanedCode,
                ["hasProxy"] = proxyConfig != null,
                ["proxyType"] = proxyConfig?.Type ?? "none"
            });

            var jsonContent = JsonSerializer.Serialize(parameters);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "claude-cli/1.0.56 (external, cli)");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
            httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            httpClient.DefaultRequestHeaders.Add("Referer", "https://claude.ai/");
            httpClient.DefaultRequestHeaders.Add("Origin", "https://claude.ai");

            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var response = await httpClient.PostAsync(OAuthConfig.TokenUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("❌ OAuth token exchange failed with server error", new Dictionary<string, object>
                {
                    ["status"] = (int)response.StatusCode,
                    ["statusText"] = response.ReasonPhrase ?? "",
                    ["data"] = errorContent,
                    ["codeLength"] = cleanedCode.Length,
                    ["codePrefix"] = cleanedCode.Length > 10 ? cleanedCode[..10] + "..." : cleanedCode
                });

                var errorMessage = $"HTTP {(int)response.StatusCode}";
                if (!string.IsNullOrEmpty(errorContent))
                    try
                    {
                        using var errorDoc = JsonDocument.Parse(errorContent);
                        if (errorDoc.RootElement.TryGetProperty("error", out var error))
                        {
                            errorMessage += $": {error.GetString()}";
                            if (errorDoc.RootElement.TryGetProperty("error_description", out var description))
                                errorMessage += $" - {description.GetString()}";
                        }
                        else
                        {
                            errorMessage += $": {errorContent}";
                        }
                    }
                    catch
                    {
                        errorMessage += $": {errorContent}";
                    }

                throw new Exception($"Token exchange failed: {errorMessage}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(responseContent);
            var root = document.RootElement;

            _logger.LogInformation("✅ OAuth token exchange successful", new Dictionary<string, object>
            {
                ["status"] = (int)response.StatusCode,
                ["hasAccessToken"] = root.TryGetProperty("access_token", out _),
                ["hasRefreshToken"] = root.TryGetProperty("refresh_token", out _),
                ["scopes"] = root.TryGetProperty("scope", out var scopeElement) ? scopeElement.GetString() : ""
            });

            var accessToken = root.GetProperty("access_token").GetString() ?? "";
            var refreshToken = root.GetProperty("refresh_token").GetString() ?? "";
            var expiresIn = root.TryGetProperty("expires_in", out var expiresElement)
                ? expiresElement.GetInt64()
                : 3600;
            var scopeString = root.TryGetProperty("scope", out var scope) ? scope.GetString() : OAuthConfig.Scopes;

            return new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTimeOffset.Now.ToUnixTimeSeconds() + expiresIn,
                Scopes = scopeString?.Split(' ') ?? new[] { "user:inference", "user:profile" },
                IsMax = true
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError("❌ OAuth token exchange failed with network error", new Dictionary<string, object>
            {
                ["message"] = ex.Message,
                ["hasProxy"] = proxyConfig != null
            });
            throw new Exception("Token exchange failed: No response from server (network error or timeout)");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError("❌ OAuth token exchange failed with timeout", new Dictionary<string, object>
            {
                ["message"] = ex.Message,
                ["hasProxy"] = proxyConfig != null
            });
            throw new Exception("Token exchange failed: Request timed out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ OAuth token exchange failed with unknown error");
            throw new Exception($"Token exchange failed: {ex.Message}");
        }
    }

    public string ParseCallbackUrl(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) throw new ArgumentException("请提供有效的授权码或回调 URL");

        var trimmedInput = input.Trim();

        if (trimmedInput.StartsWith("http://") || trimmedInput.StartsWith("https://"))
            try
            {
                var uri = new Uri(trimmedInput);
                var query = HttpUtility.ParseQueryString(uri.Query);
                var authorizationCode = query["code"];

                if (string.IsNullOrEmpty(authorizationCode)) throw new ArgumentException("回调 URL 中未找到授权码 (code 参数)");

                return authorizationCode;
            }
            catch (UriFormatException)
            {
                throw new ArgumentException("无效的 URL 格式，请检查回调 URL 是否正确");
            }

        var cleanedCode = trimmedInput.Split('#')[0]?.Split('&')[0] ?? trimmedInput;

        if (string.IsNullOrEmpty(cleanedCode) || cleanedCode.Length < 10)
            throw new ArgumentException("授权码格式无效，请确保复制了完整的 Authorization Code");

        var validCodePattern = new Regex(@"^[A-Za-z0-9_-]+$");
        if (!validCodePattern.IsMatch(cleanedCode))
            throw new ArgumentException("授权码包含无效字符，请检查是否复制了正确的 Authorization Code");

        return cleanedCode;
    }

    public ClaudeCredentials FormatClaudeCredentials(TokenResponse tokenData)
    {
        return new ClaudeCredentials
        {
            ClaudeAiOauth = new ClaudeAiOAuth
            {
                AccessToken = tokenData.AccessToken,
                RefreshToken = tokenData.RefreshToken,
                ExpiresAt = tokenData.ExpiresAt,
                Scopes = tokenData.Scopes,
                IsMax = tokenData.IsMax
            }
        };
    }

    private HttpClient CreateHttpClientWithProxy(ProxyConfig? proxyConfig)
    {
        if (proxyConfig == null) return new HttpClient();

        try
        {
            var handler = new HttpClientHandler();
            var proxyUri = $"{proxyConfig.Type}://{proxyConfig.Host}:{proxyConfig.Port}";

            if (!string.IsNullOrEmpty(proxyConfig.Username) && !string.IsNullOrEmpty(proxyConfig.Password))
                proxyUri =
                    $"{proxyConfig.Type}://{proxyConfig.Username}:{proxyConfig.Password}@{proxyConfig.Host}:{proxyConfig.Port}";

            handler.Proxy = new WebProxy(proxyUri);
            handler.UseProxy = true;

            return new HttpClient(handler);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("⚠️ Invalid proxy configuration: {Error}", ex.Message);
            return new HttpClient();
        }
    }
}