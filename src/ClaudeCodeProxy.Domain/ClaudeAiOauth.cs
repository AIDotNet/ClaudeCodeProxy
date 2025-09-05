namespace ClaudeCodeProxy.Domain;

public class ClaudeAiOauth
{
    public string AccessToken { get; set; }

    public string RefreshToken { get; set; }

    public long ExpiresAt { get; set; }

    public string[] scopes { get; set; }

    public bool isMax { get; set; }
}