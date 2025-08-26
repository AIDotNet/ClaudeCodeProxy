namespace ClaudeCodeProxy.Domain;

/// <summary>
/// OpenAI OAuth认证信息
/// </summary>
public class OpenAiOauth
{
    /// <summary>
    /// 访问令牌
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;
    
    /// <summary>
    /// 刷新令牌
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
    
    /// <summary>
    /// 令牌过期时间戳
    /// </summary>
    public long ExpiresAt { get; set; }
    
    /// <summary>
    /// 授权范围
    /// </summary>
    public string[] Scopes { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// 是否为最大权限
    /// </summary>
    public bool IsMax { get; set; }
    
    /// <summary>
    /// 用户信息
    /// </summary>
    public OpenAiUserInfo? UserInfo { get; set; }
}

/// <summary>
/// OpenAI用户信息
/// </summary>
public class OpenAiUserInfo
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户邮箱
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户姓名
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// 头像URL
    /// </summary>
    public string? Picture { get; set; }
    
    /// <summary>
    /// 组织信息
    /// </summary>
    public OpenAiOrganization[]? Organizations { get; set; }
}

/// <summary>
/// OpenAI组织信息
/// </summary>
public class OpenAiOrganization
{
    /// <summary>
    /// 组织ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// 组织名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 组织标题
    /// </summary>
    public string? Title { get; set; }
    
    /// <summary>
    /// 用户角色
    /// </summary>
    public string? Role { get; set; }
}