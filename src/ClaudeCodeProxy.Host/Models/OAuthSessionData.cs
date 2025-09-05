using ClaudeCodeProxy.Domain;

namespace ClaudeCodeProxy.Host.Models;

public class OAuthSessionData
{
    public string CodeVerifier { get; set; }

    public string State { get; set; }

    public string CodeChallenge { get; set; }

    public ProxyConfig? Proxy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }
}