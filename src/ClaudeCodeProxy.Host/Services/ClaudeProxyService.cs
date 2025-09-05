using ClaudeCodeProxy.Domain;
using ClaudeCodeProxy.Host.Helper;
using ClaudeCodeProxy.Host.Models;
using Microsoft.Extensions.Caching.Memory;

namespace ClaudeCodeProxy.Host.Services;

public class ClaudeProxyService(IMemoryCache memory, OAuthHelper oAuthHelper)
{
    public GenerateAuthUrlResult GenerateAuthUrl(GenerateAuthUrlInput input)
    {
        var oauthParams = oAuthHelper.GenerateOAuthParams();

        // 生成会话ID用于临时存储
        var sessionId = Guid.NewGuid().ToString();

        // 将OAuth参数存储到缓存中,10分钟过期
        var cacheEntry = new OAuthSessionData
        {
            CodeVerifier = oauthParams.CodeVerifier,
            State = oauthParams.State,
            CodeChallenge = oauthParams.CodeChallenge,
            Proxy = input.Proxy,
            CreatedAt = DateTime.Now,
            ExpiresAt = DateTime.Now.AddMinutes(10)
        };

        memory.Set(sessionId, cacheEntry, TimeSpan.FromMinutes(10));

        return new GenerateAuthUrlResult
        {
            Instructions =
            [
                "1. 复制上面的链接到浏览器中打开",
                "2. 登录您的 Anthropic 账户",
                "3. 同意应用权限",
                "4. 复制浏览器地址栏中的完整 URL",
                "5. 在添加账户表单中粘贴完整的回调 URL 和授权码"
            ],
            AuthUrl = oauthParams.AuthUrl,
            SessionId = sessionId
        };
    }

    public async Task<object> ExchangeCode(ExchangeCodeInput input)
    {
        // 验证必需参数
        if (string.IsNullOrEmpty(input.SessionId) ||
            (string.IsNullOrEmpty(input.AuthorizationCode) && string.IsNullOrEmpty(input.CallbackUrl)))
            throw new ArgumentException("Session ID and authorization code (or callback URL) are required");

        // 从内存缓存获取OAuth会话信息
        if (!memory.TryGetValue(input.SessionId, out OAuthSessionData? oauthSession) || oauthSession == null)
            throw new InvalidOperationException("Invalid or expired OAuth session");

        // 检查会话是否过期
        if (DateTime.Now > oauthSession.ExpiresAt)
        {
            memory.Remove(input.SessionId);
            throw new InvalidOperationException("OAuth session has expired, please generate a new authorization URL");
        }

        // 统一处理授权码输入（可能是直接的code或完整的回调URL）
        string finalAuthCode;
        var inputValue = input.CallbackUrl ?? input.AuthorizationCode;

        try
        {
            finalAuthCode = oAuthHelper.ParseCallbackUrl(inputValue!);
        }
        catch (Exception parseError)
        {
            throw new ArgumentException($"Failed to parse authorization input: {parseError.Message}");
        }

        // 转换ProxyAuth到ProxyConfig
        ProxyConfig? proxyConfig = null;
        if (!string.IsNullOrEmpty(oauthSession.Proxy?.Host)) proxyConfig = oauthSession.Proxy;

        // 交换访问令牌
        var tokenData = await oAuthHelper.ExchangeCodeForTokensAsync(
            finalAuthCode,
            oauthSession.CodeVerifier,
            oauthSession.State,
            proxyConfig
        );

        // 清理OAuth会话
        memory.Remove(input.SessionId);

        // 格式化Claude凭据
        var claudeCredentials = oAuthHelper.FormatClaudeCredentials(tokenData);

        return new
        {
            success = true,
            data = new
            {
                claudeAiOauth = claudeCredentials.ClaudeAiOauth
            }
        };
    }
}