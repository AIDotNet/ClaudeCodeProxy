using ClaudeCodeProxy.Host.Models;
using ClaudeCodeProxy.Host.Services;
using Making.Jwt.Models;
using Making.Jwt.Services;

namespace ClaudeCodeProxy.Host.Endpoints;

/// <summary>
///     OAuth第三方登录端点
/// </summary>
public static class OAuthEndpoints
{
    public static void MapOAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/oauth")
            .WithTags("第三方登录");

        // 获取OAuth提供商配置状态
        group.MapGet("/providers/status", (IConfiguration configuration) =>
            {
                var githubConfig = configuration.GetSection("OAuth:GitHub").Get<OAuthService.GitHubConfig>();
                var giteeConfig = configuration.GetSection("OAuth:Gitee").Get<OAuthService.GiteeConfig>();
                var googleConfig = configuration.GetSection("OAuth:Google").Get<OAuthService.GoogleConfig>();

                return Results.Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new
                    {
                        github = !string.IsNullOrEmpty(githubConfig?.ClientId),
                        gitee = !string.IsNullOrEmpty(giteeConfig?.ClientId),
                        google = !string.IsNullOrEmpty(googleConfig?.ClientId)
                    },
                    Message = "获取OAuth提供商状态成功"
                });
            })
            .WithName("GetOAuthProvidersStatus")
            .WithSummary("获取OAuth提供商配置状态");

        // GitHub登录授权URL
        group.MapGet("/github/auth", (string redirectUri, string? state, OAuthService oAuthService) =>
            {
                try
                {
                    var authUrl = oAuthService.GenerateGitHubAuthUrl(redirectUri, state);
                    return Results.Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Data = new { authUrl },
                        Message = "生成GitHub授权URL成功"
                    });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = ex.Message
                    });
                }
            })
            .WithName("GitHubAuthUrl")
            .WithSummary("获取GitHub登录授权URL");

        // GitHub登录回调
        group.MapPost("/github/callback", async (GitHubCallbackRequest request, OAuthService oAuthService,
                UserService userService, IJwtService jwtService, AuthService authService,
                IInvitationService invitationService, HttpContext context) =>
            {
                var userInfo = await oAuthService.HandleGitHubCallbackAsync(request.Code, request.RedirectUri);
                if (userInfo == null)
                    return Results.BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "GitHub登录失败"
                    });

                // 创建或更新用户
                var user = await userService.CreateOrUpdateOAuthUserAsync(
                    userInfo.Provider, userInfo.ProviderId, userInfo.Email, userInfo.Name, userInfo.Avatar,
                    request.InvitationCode, invitationService);

                // 记录登录历史
                var ipAddress = context.Connection.RemoteIpAddress?.ToString();
                var userAgent = context.Request.Headers.UserAgent.ToString();
                await userService.RecordLoginHistoryAsync(user.Id, "github", ipAddress, userAgent, true);

                // 生成JWT令牌
                var claims = new JwtClaims
                {
                    UserId = user.Id,
                    UserName = user.Username,
                    Email = user.Email,
                    Roles = [user.Role.Name]
                };

                var tokenResult = await jwtService.GenerateTokenAsync(claims);

                return Results.Ok(new ApiResponse<LoginResponse>
                {
                    Success = true,
                    Data = new LoginResponse
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
                    },
                    Message = "GitHub登录成功"
                });
            })
            .WithName("GitHubCallback")
            .WithSummary("GitHub登录回调");

        // Gitee登录授权URL
        group.MapGet("/gitee/auth", (string redirectUri, string? state, OAuthService oAuthService) =>
            {
                try
                {
                    var authUrl = oAuthService.GenerateGiteeAuthUrl(redirectUri, state);
                    return Results.Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Data = new { authUrl },
                        Message = "生成Gitee授权URL成功"
                    });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = ex.Message
                    });
                }
            })
            .WithName("GiteeAuthUrl")
            .WithSummary("获取Gitee登录授权URL");

        // Gitee登录回调
        group.MapPost("/gitee/callback", async (GiteeCallbackRequest request, OAuthService oAuthService,
                UserService userService, IJwtService jwtService, AuthService authService,
                IInvitationService invitationService, HttpContext context) =>
            {
                try
                {
                    var userInfo = await oAuthService.HandleGiteeCallbackAsync(request.Code, request.RedirectUri);
                    if (userInfo == null)
                        return Results.BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = "Gitee登录失败"
                        });

                    // 创建或更新用户
                    var user = await userService.CreateOrUpdateOAuthUserAsync(
                        userInfo.Provider, userInfo.ProviderId, userInfo.Email, userInfo.Name, userInfo.Avatar,
                        request.InvitationCode, invitationService);

                    // 记录登录历史
                    var ipAddress = context.Connection.RemoteIpAddress?.ToString();
                    var userAgent = context.Request.Headers.UserAgent.ToString();
                    await userService.RecordLoginHistoryAsync(user.Id, "gitee", ipAddress, userAgent, true);

                    // 生成JWT令牌
                    var claims = new JwtClaims
                    {
                        UserId = user.Id,
                        UserName = user.Username,
                        Email = user.Email,
                        Roles = [user.Role.Name]
                    };

                    var tokenResult = await jwtService.GenerateTokenAsync(claims);

                    return Results.Ok(new ApiResponse<LoginResponse>
                    {
                        Success = true,
                        Data = new LoginResponse
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
                        },
                        Message = "Gitee登录成功"
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        $"Gitee登录失败: {ex.Message}",
                        statusCode: 500
                    );
                }
            })
            .WithName("GiteeCallback")
            .WithSummary("Gitee登录回调");

        // Google登录授权URL
        group.MapGet("/google/auth", (string redirectUri, string? state, OAuthService oAuthService) =>
            {
                try
                {
                    var authUrl = oAuthService.GenerateGoogleAuthUrl(redirectUri, state);
                    return Results.Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Data = new { authUrl },
                        Message = "生成Google授权URL成功"
                    });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = ex.Message
                    });
                }
            })
            .WithName("GoogleAuthUrl")
            .WithSummary("获取Google登录授权URL");

        // Google登录回调
        group.MapPost("/google/callback", async (GoogleCallbackRequest request, OAuthService oAuthService,
                UserService userService, IJwtService jwtService, AuthService authService,
                IInvitationService invitationService, HttpContext context) =>
            {
                try
                {
                    var userInfo = await oAuthService.HandleGoogleCallbackAsync(request.Code, request.RedirectUri);
                    if (userInfo == null)
                        return Results.BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = "Google登录失败"
                        });

                    // 创建或更新用户
                    var user = await userService.CreateOrUpdateOAuthUserAsync(
                        userInfo.Provider, userInfo.ProviderId, userInfo.Email, userInfo.Name, userInfo.Avatar,
                        request.InvitationCode, invitationService);

                    // 记录登录历史
                    var ipAddress = context.Connection.RemoteIpAddress?.ToString();
                    var userAgent = context.Request.Headers.UserAgent.ToString();
                    await userService.RecordLoginHistoryAsync(user.Id, "google", ipAddress, userAgent, true);

                    // 生成JWT令牌
                    var claims = new JwtClaims
                    {
                        UserId = user.Id,
                        UserName = user.Username,
                        Email = user.Email,
                        Roles = [user.Role.Name]
                    };

                    var tokenResult = await jwtService.GenerateTokenAsync(claims);

                    return Results.Ok(new ApiResponse<LoginResponse>
                    {
                        Success = true,
                        Data = new LoginResponse
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
                        },
                        Message = "Google登录成功"
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        $"Google登录失败: {ex.Message}",
                        statusCode: 500
                    );
                }
            })
            .WithName("GoogleCallback")
            .WithSummary("Google登录回调");
    }

    /// <summary>
    ///     GitHub回调请求
    /// </summary>
    public class GitHubCallbackRequest
    {
        public string Code { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public string? State { get; set; }
        public string? InvitationCode { get; set; }
    }

    /// <summary>
    ///     Gitee回调请求
    /// </summary>
    public class GiteeCallbackRequest
    {
        public string Code { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public string? State { get; set; }
        public string? InvitationCode { get; set; }
    }

    /// <summary>
    ///     Google回调请求
    /// </summary>
    public class GoogleCallbackRequest
    {
        public string Code { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public string? State { get; set; }
        public string? InvitationCode { get; set; }
    }
}