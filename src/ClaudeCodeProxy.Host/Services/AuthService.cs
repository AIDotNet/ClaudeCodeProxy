using System.Security.Claims;
using ClaudeCodeProxy.Host.Models;
using Making.Jwt.Models;
using Making.Jwt.Services;

namespace ClaudeCodeProxy.Host.Services;

/// <summary>
///     认证服务实现
/// </summary>
public class AuthService(IJwtService jwtService, UserService userService, IInvitationService invitationService)
{
    /// <summary>
    ///     用户登录
    /// </summary>
    /// <param name="request">登录请求</param>
    /// <param name="ipAddress">IP地址</param>
    /// <param name="userAgent">用户代理</param>
    /// <returns>登录响应</returns>
    public async Task<LoginResponse?> LoginAsync(LoginRequest request, string? ipAddress = null,
        string? userAgent = null)
    {
        // 首先尝试数据库用户验证
        var user = await userService.ValidateUserAsync(request.Username, request.Password);

        if (user != null)
        {
            // 记录登录历史
            await userService.RecordLoginHistoryAsync(user.Id, "password", ipAddress, userAgent, true);

            // 创建JWT声明
            var claims = new JwtClaims
            {
                UserId = user.Id, // 生成一个新的Guid，但在NameIdentifier中存储真实的用户ID
                UserName = user.Username,
                Email = user.Email,
                Roles = [user.Role.Name],
                // 添加自定义声明来存储真实的用户ID
                CustomClaims = new Dictionary<string, object>
                {
                    [ClaimTypes.NameIdentifier] = user.Id.ToString()
                }
            };

            // 生成JWT令牌
            var tokenResult = await jwtService.GenerateTokenAsync(claims);

            return new LoginResponse
            {
                AccessToken = tokenResult.AccessToken,
                RefreshToken = tokenResult.RefreshToken,
                TokenType = "Bearer",
                ExpiresIn = (int)TimeSpan.FromDays(7).TotalSeconds,
                Username = user.Username,
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Avatar = user.Avatar,
                    IsActive = user.IsActive,
                    EmailConfirmed = user.EmailConfirmed,
                    LastLoginAt = user.LastLoginAt,
                    Provider = user.Provider,
                    ProviderId = user.ProviderId,
                    RoleId = user.RoleId,
                    RoleName = user.Role.Name,
                    CreatedAt = user.CreatedAt,
                    ModifiedAt = user.ModifiedAt
                }
            };
        }

        throw new ApplicationException("无效的用户名或密码");
    }

    /// <summary>
    ///     用户注册
    /// </summary>
    /// <param name="request">注册请求</param>
    /// <param name="ipAddress">IP地址</param>
    /// <param name="userAgent">用户代理</param>
    /// <returns>注册响应</returns>
    public async Task<LoginResponse?> RegisterAsync(RegisterUserRequest request, string? ipAddress = null,
        string? userAgent = null)
    {
        try
        {
            // 注册用户
            var user = await userService.RegisterUserAsync(request, invitationService);

            // 记录登录历史
            await userService.RecordLoginHistoryAsync(user.Id, "register", ipAddress, userAgent, true);

            // 创建JWT声明
            var claims = new JwtClaims
            {
                UserId = user.Id,
                UserName = user.Username,
                Email = user.Email,
                Roles = [user.RoleName],
                // 添加自定义声明来存储真实的用户ID
                CustomClaims = new Dictionary<string, object>
                {
                    [ClaimTypes.NameIdentifier] = user.Id.ToString()
                }
            };

            // 生成JWT令牌
            var tokenResult = await jwtService.GenerateTokenAsync(claims);

            return new LoginResponse
            {
                AccessToken = tokenResult.AccessToken,
                RefreshToken = tokenResult.RefreshToken,
                TokenType = "Bearer",
                ExpiresIn = (int)TimeSpan.FromDays(7).TotalSeconds,
                Username = user.Username,
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Avatar = user.Avatar,
                    IsActive = user.IsActive,
                    EmailConfirmed = user.EmailConfirmed,
                    LastLoginAt = user.LastLoginAt,
                    Provider = user.Provider,
                    ProviderId = user.ProviderId,
                    RoleId = user.RoleId,
                    RoleName = user.RoleName,
                    CreatedAt = user.CreatedAt,
                    ModifiedAt = user.ModifiedAt
                }
            };
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"注册失败: {ex.Message}", ex);
        }
    }
}