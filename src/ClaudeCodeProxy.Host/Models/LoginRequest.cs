namespace ClaudeCodeProxy.Host.Models;

/// <summary>
///     登录请求模型
/// </summary>
public class LoginRequest
{
    /// <summary>
    ///     用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    ///     密码
    /// </summary>
    public string Password { get; set; } = string.Empty;
}