using ClaudeCodeProxy.Core;
using ClaudeCodeProxy.Domain;
using ClaudeCodeProxy.Host.Models;
using ClaudeCodeProxy.Host.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ClaudeCodeProxy.Host.Endpoints;

/// <summary>
///     API Key 相关端点
/// </summary>
public static class ApiKeyEndpoints
{
    /// <summary>
    ///     配置API Key相关路由
    /// </summary>
    public static void MapApiKeyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/apikeys")
            .WithTags("ApiKey")
            .WithOpenApi();

        // 获取所有API Keys
        group.MapGet("/", GetApiKeys)
            .WithName("GetApiKeys")
            .WithSummary("获取所有API Keys")
            .Produces<ApiResponse<List<ApiKey>>>();

        // 根据ID获取API Key
        group.MapGet("/{id:guid}", GetApiKeyById)
            .WithName("GetApiKeyById")
            .WithSummary("根据ID获取API Key")
            .Produces<ApiResponse<ApiKey>>()
            .Produces<ApiResponse>(404);

        // 创建新的API Key
        group.MapPost("/", CreateApiKey)
            .WithName("CreateApiKey")
            .WithSummary("创建新的API Key")
            .Produces<ApiResponse<ApiKey>>(201)
            .Produces<ApiResponse>(400);

        // 更新API Key
        group.MapPut("/{id:guid}", UpdateApiKey)
            .WithName("UpdateApiKey")
            .WithSummary("更新API Key")
            .Produces<ApiResponse<ApiKey>>()
            .Produces<ApiResponse>(404)
            .Produces<ApiResponse>(400);

        // 删除API Key
        group.MapDelete("/{id:guid}", DeleteApiKey)
            .WithName("DeleteApiKey")
            .WithSummary("删除API Key")
            .Produces<ApiResponse>(204)
            .Produces<ApiResponse>(404);

        // 启用API Key
        group.MapPatch("/{id:guid}/enable", EnableApiKey)
            .WithName("EnableApiKey")
            .WithSummary("启用API Key")
            .Produces<ApiResponse>()
            .Produces<ApiResponse>(404);

        // 禁用API Key
        group.MapPatch("/{id:guid}/disable", DisableApiKey)
            .WithName("DisableApiKey")
            .WithSummary("禁用API Key")
            .Produces<ApiResponse>()
            .Produces<ApiResponse>(404);

        // 切换API Key启用状态
        group.MapPatch("/{id:guid}/toggle", ToggleApiKeyEnabled)
            .WithName("ToggleApiKeyEnabled")
            .WithSummary("切换API Key启用状态")
            .Produces<ApiResponse>()
            .Produces<ApiResponse>(404);

        // 验证API Key
        group.MapPost("/validate", ValidateApiKey)
            .WithName("ValidateApiKey")
            .WithSummary("验证API Key")
            .Produces<ApiResponse<bool>>();

        // 获取API Key费用使用情况
        group.MapGet("/{id:guid}/usage", GetApiKeyUsage)
            .WithName("GetApiKeyUsage")
            .WithSummary("获取API Key费用使用情况")
            .Produces<ApiResponse<CostUsageInfo>>()
            .Produces<ApiResponse>(404);

        // 获取API Key账户绑定管理信息
        group.MapGet("/{id:guid}/account-bindings", GetApiKeyAccountBindings)
            .WithName("GetApiKeyAccountBindings")
            .WithSummary("获取API Key账户绑定管理信息")
            .Produces<ApiResponse<ApiKeyBindingManagementDto>>()
            .Produces<ApiResponse>(404);

        // 设置API Key默认账户
        group.MapPut("/{id:guid}/default-account", SetApiKeyDefaultAccount)
            .WithName("SetApiKeyDefaultAccount")
            .WithSummary("设置API Key默认账户")
            .Produces<ApiResponse>()
            .Produces<ApiResponse>(404)
            .Produces<ApiResponse>(400);

        // 批量更新API Key账户绑定
        group.MapPut("/{id:guid}/account-bindings", UpdateApiKeyAccountBindings)
            .WithName("UpdateApiKeyAccountBindings")
            .WithSummary("批量更新API Key账户绑定")
            .Produces<ApiResponse>()
            .Produces<ApiResponse>(404)
            .Produces<ApiResponse>(400);
    }

    /// <summary>
    ///     获取所有API Keys
    /// </summary>
    private static async Task<List<ApiKey>> GetApiKeys(
        [FromServices] IUserContext userContext,
        ApiKeyService apiKeyService)
    {
        var apiKeys = await apiKeyService.GetAllApiKeysAsync(userContext);

        apiKeys.ForEach(x => { x.User.ApiKeys = null; });
        return apiKeys;
    }

    /// <summary>
    ///     根据ID获取API Key
    /// </summary>
    private static async Task<Results<Ok<ApiKey>, NotFound<string>>> GetApiKeyById(
        Guid id,
        ApiKeyService apiKeyService)
    {
        try
        {
            var apiKey = await apiKeyService.GetApiKeyByIdAsync(id);
            if (apiKey == null) return TypedResults.NotFound($"未找到ID为 {id} 的API Key");

            return TypedResults.Ok(apiKey);
        }
        catch (Exception ex)
        {
            return TypedResults.NotFound($"获取API Key失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     创建新的API Key
    /// </summary>
    private static async Task<Results<Created<ApiKey>, BadRequest<string>, UnauthorizedHttpResult>> CreateApiKey(
        CreateApiKeyRequest request,
        ApiKeyService apiKeyService,
        IUserContext userContext)
    {
        try
        {
            var userId = userContext.GetCurrentUserId();
            if (userId == null) return TypedResults.Unauthorized();

            var apiKey = await apiKeyService.CreateApiKeyAsync(request, userId.Value);
            return TypedResults.Created($"/api/apikeys/{apiKey.Id}", apiKey);
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest($"创建API Key失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     更新API Key
    /// </summary>
    private static async Task<Results<Ok<ApiKey>, NotFound<string>, BadRequest<string>>> UpdateApiKey(
        Guid id,
        UpdateApiKeyRequest request,
        ApiKeyService apiKeyService)
    {
        try
        {
            var apiKey = await apiKeyService.UpdateApiKeyAsync(id, request);
            if (apiKey == null) return TypedResults.NotFound($"未找到ID为 {id} 的API Key");

            return TypedResults.Ok(apiKey);
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest($"更新API Key失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     删除API Key
    /// </summary>
    private static async Task<Results<NoContent, NotFound<string>>> DeleteApiKey(
        Guid id,
        ApiKeyService apiKeyService)
    {
        try
        {
            var success = await apiKeyService.DeleteApiKeyAsync(id);
            if (!success) return TypedResults.NotFound($"未找到ID为 {id} 的API Key");

            return TypedResults.NoContent();
        }
        catch (Exception ex)
        {
            return TypedResults.NotFound($"删除API Key失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     验证API Key
    /// </summary>
    private static async Task<Results<Ok<bool>, BadRequest<string>>> ValidateApiKey(
        ValidateApiKeyRequest request,
        ApiKeyService apiKeyService)
    {
        try
        {
            var isValid = await apiKeyService.ValidateApiKeyAsync(request.Key);
            return TypedResults.Ok(isValid);
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest($"验证API Key失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     启用API Key
    /// </summary>
    private static async Task<Results<Ok, NotFound<string>>> EnableApiKey(
        Guid id,
        ApiKeyService apiKeyService)
    {
        try
        {
            var success = await apiKeyService.EnableApiKeyAsync(id);
            if (!success) return TypedResults.NotFound($"未找到ID为 {id} 的API Key");

            return TypedResults.Ok();
        }
        catch (Exception ex)
        {
            return TypedResults.NotFound($"启用API Key失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     禁用API Key
    /// </summary>
    private static async Task<Results<Ok, NotFound<string>>> DisableApiKey(
        Guid id,
        ApiKeyService apiKeyService)
    {
        try
        {
            var success = await apiKeyService.DisableApiKeyAsync(id);
            if (!success) return TypedResults.NotFound($"未找到ID为 {id} 的API Key");

            return TypedResults.Ok();
        }
        catch (Exception ex)
        {
            return TypedResults.NotFound($"禁用API Key失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     切换API Key启用状态
    /// </summary>
    private static async Task<Results<Ok, NotFound<string>>> ToggleApiKeyEnabled(
        Guid id,
        ApiKeyService apiKeyService)
    {
        try
        {
            var success = await apiKeyService.ToggleApiKeyEnabledAsync(id);
            if (!success) return TypedResults.NotFound($"未找到ID为 {id} 的API Key");

            return TypedResults.Ok();
        }
        catch (Exception ex)
        {
            return TypedResults.NotFound($"切换API Key状态失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     获取API Key费用使用情况
    /// </summary>
    private static async Task<Results<Ok<CostUsageInfo>, NotFound<string>>> GetApiKeyUsage(
        Guid id,
        ApiKeyService apiKeyService)
    {
        try
        {
            var apiKey = await apiKeyService.GetApiKeyByIdAsync(id);
            if (apiKey == null) return TypedResults.NotFound($"未找到ID为 {id} 的API Key");

            var costUsage = apiKey.GetCostUsage();
            return TypedResults.Ok(costUsage);
        }
        catch (Exception ex)
        {
            return TypedResults.NotFound($"获取API Key使用情况失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     获取API Key账户绑定管理信息
    /// </summary>
    private static async Task<Results<Ok<ApiResponse<ApiKeyBindingManagementDto>>, NotFound<ApiResponse<object>>>>
        GetApiKeyAccountBindings(
            Guid id,
            ApiKeyService apiKeyService,
            IUserContext userContext)
    {
        try
        {
            var managementDto = await apiKeyService.GetApiKeyBindingManagementAsync(id);
            if (managementDto == null)
                return TypedResults.NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"未找到ID为 {id} 的API Key"
                });

            return TypedResults.Ok(new ApiResponse<ApiKeyBindingManagementDto>
            {
                Success = true,
                Data = managementDto,
                Message = "获取API Key账户绑定信息成功"
            });
        }
        catch (Exception ex)
        {
            return TypedResults.NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = $"获取API Key账户绑定信息失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    ///     设置API Key默认账户
    /// </summary>
    private static async
        Task<Results<Ok<ApiResponse<object>>, NotFound<ApiResponse<object>>, BadRequest<ApiResponse<object>>>>
        SetApiKeyDefaultAccount(
            Guid id,
            SetDefaultAccountRequest request,
            ApiKeyService apiKeyService)
    {
        try
        {
            var success = await apiKeyService.SetDefaultAccountAsync(id, request.AccountId);
            if (!success)
                return TypedResults.NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"未找到ID为 {id} 的API Key"
                });

            return TypedResults.Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "设置默认账户成功"
            });
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = $"设置默认账户失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    ///     批量更新API Key账户绑定
    /// </summary>
    private static async
        Task<Results<Ok<ApiResponse<object>>, NotFound<ApiResponse<object>>, BadRequest<ApiResponse<object>>>>
        UpdateApiKeyAccountBindings(
            Guid id,
            UpdateApiKeyAccountBindingsRequest request,
            ApiKeyService apiKeyService)
    {
        try
        {
            var success =
                await apiKeyService.UpdateAccountBindingsAsync(id, request.DefaultAccountId, request.AccountBindings);
            if (!success)
                return TypedResults.NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"未找到ID为 {id} 的API Key"
                });

            return TypedResults.Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "更新账户绑定成功"
            });
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = $"更新账户绑定失败: {ex.Message}"
            });
        }
    }
}