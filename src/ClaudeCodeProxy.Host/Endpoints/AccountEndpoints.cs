using ClaudeCodeProxy.Core;
using ClaudeCodeProxy.Domain;
using ClaudeCodeProxy.Host.Services;
using ClaudeCodeProxy.Host.Models;
using ClaudeCodeProxy.Host.Helper;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ClaudeCodeProxy.Host.Endpoints;

/// <summary>
/// 账户相关端点
/// </summary>
public static class AccountEndpoints
{
    /// <summary>
    /// 配置账户相关路由
    /// </summary>
    public static void MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/accounts")
            .WithTags("Account")
            .WithOpenApi();

        // 获取所有账户
        group.MapGet("/", GetAccounts)
            .WithName("GetAccounts")
            .WithSummary("获取所有账户")
            .Produces<List<Accounts>>();

        // 根据ID获取账户
        group.MapGet("/{id}", GetAccountById)
            .WithName("GetAccountById")
            .WithSummary("根据ID获取账户")
            .Produces<Accounts>()
            .Produces(404);

        // 创建新账户
        group.MapPost("/{platform}", CreateAccount)
            .WithName("CreateAccount")
            .WithSummary("创建新账户")
            .Produces<Accounts>(201)
            .Produces(400);

        // 更新账户
        group.MapPut("/{id}", UpdateAccount)
            .WithName("UpdateAccount")
            .WithSummary("更新账户")
            .Produces<Accounts>()
            .Produces(404)
            .Produces(400);

        // 删除账户
        group.MapDelete("/{id}", DeleteAccount)
            .WithName("DeleteAccount")
            .WithSummary("删除账户")
            .Produces(204)
            .Produces(404);

        // 根据平台获取账户
        group.MapGet("/platform/{platform}", GetAccountsByPlatform)
            .WithName("GetAccountsByPlatform")
            .WithSummary("根据平台获取账户")
            .Produces<List<Accounts>>();

        // 获取可用账户
        group.MapGet("/available", GetAvailableAccounts)
            .WithName("GetAvailableAccounts")
            .WithSummary("获取可用账户")
            .Produces<List<Accounts>>();

        // 更新账户状态
        group.MapPatch("/{id}/status", UpdateAccountStatus)
            .WithName("UpdateAccountStatus")
            .WithSummary("更新账户状态")
            .Produces(200)
            .Produces(404);

        // 启用账户
        group.MapPatch("/{id}/enable", EnableAccount)
            .WithName("EnableAccount")
            .WithSummary("启用账户")
            .Produces(200)
            .Produces(404);

        // 禁用账户
        group.MapPatch("/{id}/disable", DisableAccount)
            .WithName("DisableAccount")
            .WithSummary("禁用账户")
            .Produces(200)
            .Produces(404);

        // 切换账户启用状态
        group.MapPatch("/{id}/toggle", ToggleAccountEnabled)
            .WithName("ToggleAccountEnabled")
            .WithSummary("切换账户启用状态")
            .Produces(200)
            .Produces(404);

        // 更新账户使用时间
        group.MapPatch("/{id}/usage", UpdateAccountUsage)
            .WithName("UpdateAccountUsage")
            .WithSummary("更新账户使用时间")
            .Produces(200)
            .Produces(404);

        // 平台特定的更新端点
        // 更新Claude账户
        group.MapPut("/claude/{id}", UpdateClaudeAccount)
            .WithName("UpdateClaudeAccount")
            .WithSummary("更新Claude账户")
            .Produces<Accounts>()
            .Produces(404)
            .Produces(400);

        // 更新Claude Console账户
        group.MapPut("/claude-console/{id}", UpdateClaudeConsoleAccount)
            .WithName("UpdateClaudeConsoleAccount")
            .WithSummary("更新Claude Console账户")
            .Produces<Accounts>()
            .Produces(404)
            .Produces(400);

        // 更新Gemini账户
        group.MapPut("/gemini/{id}", UpdateGeminiAccount)
            .WithName("UpdateGeminiAccount")
            .WithSummary("更新Gemini账户")
            .Produces<Accounts>()
            .Produces(404)
            .Produces(400);

        // OpenAI OAuth相关端点
        // 生成OpenAI OAuth授权链接
        group.MapPost("/openai/oauth/generate-auth-url", GenerateOpenAiOAuthUrl)
            .WithName("GenerateOpenAiOAuthUrl")
            .WithSummary("生成OpenAI OAuth授权链接")
            .Produces<object>(200)
            .Produces(400);

        // 处理OpenAI OAuth授权码
        group.MapPost("/openai/oauth/exchange-code", ExchangeOpenAiOAuthCode)
            .WithName("ExchangeOpenAiOAuthCode")
            .WithSummary("处理OpenAI OAuth授权码并创建账户")
            .Produces<Accounts>(201)
            .Produces(400);
    }

    /// <summary>
    /// 获取所有账户（支持用户权限过滤）
    /// </summary>
    private static async Task<Results<Ok<List<Accounts>>, BadRequest<string>>> GetAccounts(
        AccountsService accountsService,
        IUserContext userContext,
        UserAccountBindingService? bindingService = null,
        HttpContext? httpContext = null)
    {
        try
        {
            var userId = userContext.GetCurrentUserId()!.Value; // 需要从认证信息获取
            var isAdmin = userContext.IsAdmin();

            List<Accounts> accounts;
            
            if (bindingService != null && userId != Guid.Empty && !isAdmin)
            {
                // 普通用户只能看到有权限访问的账户
                accounts = await bindingService.GetVisibleAccountsForUserAsync(userId, isAdmin);
            }
            else
            {
                // 管理员可以看到所有账户
                accounts = await accountsService.GetAllAccountsAsync();
            }
            
            return TypedResults.Ok(accounts);
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest($"获取账户失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 根据ID获取账户
    /// </summary>
    private static async Task<Results<Ok<Accounts>, NotFound<string>>> GetAccountById(
        string id,
        AccountsService accountsService)
    {
        try
        {
            var account = await accountsService.GetAccountByIdAsync(id);

            if (account == null)
            {
                return TypedResults.NotFound($"未找到ID为 {id} 的账户");
            }

            return TypedResults.Ok(account);
        }
        catch (Exception ex)
        {
            return TypedResults.NotFound($"获取账户失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 创建新账户
    /// </summary>
    private static async Task<Results<Created<Accounts>, BadRequest<string>>> CreateAccount(
        string platform,
        CreateAccountRequest request,
        AccountsService accountsService)
    {
        try
        {
            var account = await accountsService.CreateAccountAsync(platform, request);
            return TypedResults.Created($"/api/accounts/{account.Id}", account);
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest($"创建账户失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新账户
    /// </summary>
    private static async Task<Results<Ok<Accounts>, NotFound<string>, BadRequest<string>>> UpdateAccount(
        string id,
        UpdateAccountRequest request,
        AccountsService accountsService)
    {
        try
        {
            var account = await accountsService.UpdateAccountAsync(id, request);
            if (account == null)
            {
                return TypedResults.NotFound($"未找到ID为 {id} 的账户");
            }

            return TypedResults.Ok(account);
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest($"更新账户失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 删除账户
    /// </summary>
    private static async Task<Results<NoContent, NotFound<string>>> DeleteAccount(
        string id,
        AccountsService accountsService)
    {
        try
        {
            var success = await accountsService.DeleteAccountAsync(id);
            if (!success)
            {
                return TypedResults.NotFound($"未找到ID为 {id} 的账户");
            }

            return TypedResults.NoContent();
        }
        catch (Exception ex)
        {
            return TypedResults.NotFound($"删除账户失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 根据平台获取账户
    /// </summary>
    private static async Task<Results<Ok<List<Accounts>>, BadRequest<string>>> GetAccountsByPlatform(
        string platform,
        AccountsService accountsService)
    {
        try
        {
            var accounts = await accountsService.GetAccountsByPlatformAsync(platform);
            return TypedResults.Ok(accounts);
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest($"获取平台账户失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取可用账户
    /// </summary>
    private static async Task<Results<Ok<List<Accounts>>, BadRequest<string>>> GetAvailableAccounts(
        string? platform,
        AccountsService accountsService)
    {
        try
        {
            var accounts = await accountsService.GetAvailableAccountsAsync(platform);
            return TypedResults.Ok(accounts);
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest($"获取可用账户失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新账户状态
    /// </summary>
    private static async Task<Results<Ok, NotFound<string>>> UpdateAccountStatus(
        string id,
        string status,
        AccountsService accountsService)
    {
        try
        {
            var success = await accountsService.UpdateAccountStatusAsync(id, status);
            if (!success)
            {
                return TypedResults.NotFound($"未找到ID为 {id} 的账户");
            }

            return TypedResults.Ok();
        }
        catch (Exception ex)
        {
            return TypedResults.NotFound($"更新账户状态失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新账户使用时间
    /// </summary>
    private static async Task<Results<Ok, NotFound<string>>> UpdateAccountUsage(
        string id,
        AccountsService accountsService)
    {
        try
        {
            var success = await accountsService.UpdateLastUsedAsync(id);
            if (!success)
            {
                return TypedResults.NotFound($"未找到ID为 {id} 的账户");
            }

            return TypedResults.Ok();
        }
        catch (Exception ex)
        {
            return TypedResults.NotFound($"更新账户使用时间失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 启用账户
    /// </summary>
    private static async Task<Results<Ok, NotFound<string>>> EnableAccount(
        string id,
        AccountsService accountsService)
    {
        try
        {
            var success = await accountsService.EnableAccountAsync(id);
            if (!success)
            {
                return TypedResults.NotFound($"未找到ID为 {id} 的账户");
            }

            return TypedResults.Ok();
        }
        catch (Exception ex)
        {
            return TypedResults.NotFound($"启用账户失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 禁用账户
    /// </summary>
    private static async Task<Results<Ok, NotFound<string>>> DisableAccount(
        string id,
        AccountsService accountsService)
    {
        try
        {
            var success = await accountsService.DisableAccountAsync(id);
            if (!success)
            {
                return TypedResults.NotFound($"未找到ID为 {id} 的账户");
            }

            return TypedResults.Ok();
        }
        catch (Exception ex)
        {
            return TypedResults.NotFound($"禁用账户失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 切换账户启用状态
    /// </summary>
    private static async Task<Results<Ok, NotFound<string>>> ToggleAccountEnabled(
        string id,
        AccountsService accountsService)
    {
        try
        {
            var success = await accountsService.ToggleAccountEnabledAsync(id);
            if (!success)
            {
                return TypedResults.NotFound($"未找到ID为 {id} 的账户");
            }

            return TypedResults.Ok();
        }
        catch (Exception ex)
        {
            return TypedResults.NotFound($"切换账户状态失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新Claude账户
    /// </summary>
    private static async Task<Results<Ok<Accounts>, NotFound<string>, BadRequest<string>>> UpdateClaudeAccount(
        string id,
        UpdateAccountRequest request,
        AccountsService accountsService)
    {
        try
        {
            var account = await accountsService.UpdateAccountAsync(id, request);
            if (account == null)
            {
                return TypedResults.NotFound($"未找到ID为 {id} 的Claude账户");
            }

            return TypedResults.Ok(account);
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest($"更新Claude账户失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新Claude Console账户
    /// </summary>
    private static async Task<Results<Ok<Accounts>, NotFound<string>, BadRequest<string>>> UpdateClaudeConsoleAccount(
        string id,
        UpdateAccountRequest request,
        AccountsService accountsService)
    {
        try
        {
            var account = await accountsService.UpdateAccountAsync(id, request);
            if (account == null)
            {
                return TypedResults.NotFound($"未找到ID为 {id} 的Claude Console账户");
            }

            return TypedResults.Ok(account);
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest($"更新Claude Console账户失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新Gemini账户
    /// </summary>
    private static async Task<Results<Ok<Accounts>, NotFound<string>, BadRequest<string>>> UpdateGeminiAccount(
        string id,
        UpdateAccountRequest request,
        AccountsService accountsService)
    {
        try
        {
            var account = await accountsService.UpdateAccountAsync(id, request);
            if (account == null)
            {
                return TypedResults.NotFound($"未找到ID为 {id} 的Gemini账户");
            }

            return TypedResults.Ok(account);
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest($"更新Gemini账户失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 生成OpenAI OAuth授权链接
    /// </summary>
    private static async Task<Results<Ok<object>, BadRequest<string>>> GenerateOpenAiOAuthUrl(
        GenerateOpenAiOAuthUrlRequest request,
        OpenAiOAuthHelper oAuthHelper,
        IOAuthSessionService sessionService)
    {
        try
        {
            var oAuthParams = oAuthHelper.GenerateOAuthParams(request.ClientId, request.RedirectUri);
            
            // 将OAuth会话数据存储到缓存中，用于后续验证
            var sessionData = new OAuthSessionData
            {
                CodeVerifier = oAuthParams.CodeVerifier,
                State = oAuthParams.State,
                CodeChallenge = oAuthParams.CodeChallenge,
                Proxy = request.Proxy,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10) // 10分钟过期
            };
            
            // 存储会话数据
            sessionService.StoreSession(oAuthParams.State, sessionData);
            
            return TypedResults.Ok((object)new
            {
                authUrl = oAuthParams.AuthUrl,
                sessionId = oAuthParams.State, // 使用state作为sessionId
                state = oAuthParams.State,
                codeVerifier = oAuthParams.CodeVerifier,
                message = "请复制此链接到浏览器中进行授权，授权完成后将获得Authorization Code"
            });
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest($"生成OpenAI OAuth授权链接失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理OpenAI OAuth授权码并创建账户
    /// </summary>
    private static async Task<Results<Created<Accounts>, BadRequest<string>>> ExchangeOpenAiOAuthCode(
        ExchangeOpenAiOAuthCodeRequest request,
        OpenAiOAuthHelper oAuthHelper,
        IOAuthSessionService sessionService,
        AccountsService accountsService)
    {
        try
        {
            // 从缓存中获取OAuth会话数据
            var sessionData = sessionService.GetSession(request.SessionId);
            if (sessionData == null)
            {
                return TypedResults.BadRequest("OAuth会话已过期或无效，请重新获取授权链接");
            }

            // 使用会话数据中的参数进行token交换
            var tokenResponse = await oAuthHelper.ExchangeCodeForTokensAsync(
                request.AuthorizationCode,
                sessionData.CodeVerifier,
                sessionData.State,
                null, // 使用默认ClientId
                null, // 使用默认RedirectUri
                sessionData.Proxy ?? request.Proxy);

            // 获取用户信息
            var userInfo = await oAuthHelper.GetUserInfoAsync(tokenResponse.AccessToken, sessionData.Proxy ?? request.Proxy);

            // 格式化OAuth凭据
            var oauthCredentials = oAuthHelper.FormatOpenAiCredentials(tokenResponse, userInfo);

            // 创建账户
            var createRequest = new CreateAccountRequest(
                Name: request.AccountName ?? $"OpenAI - {userInfo.Email}",
                Description: request.Description ?? $"OpenAI账户 - {userInfo.Name}",
                ApiKey: "", // OpenAI OAuth不需要传统的API Key
                ApiUrl: "", // OpenAI OAuth不需要传统的API URL
                AccountType: request.AccountType ?? "shared",
                UserAgent: null,
                Proxy: sessionData.Proxy ?? request.Proxy,
                ClaudeAiOauth: null,
                OpenAiOauth: oauthCredentials,
                Priority: request.Priority ?? 50
            );

            var account = await accountsService.CreateAccountAsync("openai", createRequest);
            
            // 清理已使用的会话数据
            sessionService.RemoveSession(request.SessionId);
            
            return TypedResults.Created($"/api/accounts/{account.Id}", account);
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest($"处理OpenAI OAuth授权失败: {ex.Message}");
        }
    }
}