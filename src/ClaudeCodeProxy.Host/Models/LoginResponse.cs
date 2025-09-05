namespace ClaudeCodeProxy.Host.Models;

/// <summary>
///     登录响应模型
/// </summary>
public class LoginResponse
{
    /// <summary>
    ///     访问令牌
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    ///     刷新令牌
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    ///     令牌类型
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    ///     过期时间（秒）
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    ///     用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    ///     用户信息
    /// </summary>
    public UserDto? User { get; set; }
}