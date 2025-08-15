using ClaudeCodeProxy.Core;
using ClaudeCodeProxy.Domain;
using ClaudeCodeProxy.Host.Filters;
using ClaudeCodeProxy.Host.Models;
using ClaudeCodeProxy.Host.Services;

namespace ClaudeCodeProxy.Host.Endpoints;

/// <summary>
/// 用户账户绑定管理端点
/// </summary>
public static class UserAccountBindingEndpoints
{
    public static void MapUserAccountBindingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users/{userId:Guid}/account-bindings")
            .WithTags("用户账户绑定管理")
            .AddEndpointFilter<GlobalResponseFilter>()
            .RequireAuthorization();

        // 获取用户的账户绑定列表
        group.MapGet("/", async (
                Guid userId,
                UserAccountBindingService bindingService,
                bool includeGlobal = true) =>
            {
                var accounts = await bindingService.GetUserBoundAccountsAsync(userId, includeGlobal);
                return Results.Ok(new ApiResponse<List<Accounts>>
                {
                    Success = true,
                    Data = accounts,
                    Message = "获取用户账户绑定成功"
                });
            })
            .WithName("GetUserAccountBindings")
            .WithSummary("获取用户的账户绑定列表")
            .Produces<ApiResponse<List<Accounts>>>();

        // 绑定用户到账户
        group.MapPost("/", async (
                Guid userId,
                BindAccountRequest request,
                UserAccountBindingService bindingService) =>
            {
                try
                {
                    var binding = await bindingService.BindUserToAccountAsync(
                        userId,
                        request.AccountId,
                        request.Priority,
                        request.BindingType,
                        request.Remarks);

                    return Results.Ok(new ApiResponse<UserAccountBinding>
                    {
                        Success = true,
                        Data = binding,
                        Message = "账户绑定成功"
                    });
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = ex.Message
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Conflict(new ApiResponse<object>
                    {
                        Success = false,
                        Message = ex.Message
                    });
                }
            })
            .WithName("BindUserToAccount")
            .WithSummary("绑定用户到账户")
            .Produces<ApiResponse<UserAccountBinding>>(201)
            .Produces<ApiResponse<object>>(400)
            .Produces<ApiResponse<object>>(409);

        // 解除用户和账户的绑定
        group.MapDelete("/{accountId}", async (
                Guid userId,
                string accountId,
                UserAccountBindingService bindingService) =>
            {
                var result = await bindingService.UnbindUserFromAccountAsync(userId, accountId);

                if (!result)
                {
                    return Results.NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "绑定关系不存在"
                    });
                }

                return Results.Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "解除绑定成功"
                });
            })
            .WithName("UnbindUserFromAccount")
            .WithSummary("解除用户和账户的绑定")
            .Produces<ApiResponse<object>>()
            .Produces<ApiResponse<object>>(404);

        // 更新账户绑定优先级
        group.MapPut("/{accountId}/priority", async (
                Guid userId,
                string accountId,
                int priority,
                UserAccountBindingService bindingService) =>
            {
                var result = await bindingService.UpdateBindingPriorityAsync(userId, accountId, priority);

                if (!result)
                {
                    return Results.NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "绑定关系不存在"
                    });
                }

                return Results.Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "优先级更新成功"
                });
            })
            .WithName("UpdateAccountBindingPriority")
            .WithSummary("更新账户绑定优先级")
            .Produces<ApiResponse<object>>()
            .Produces<ApiResponse<object>>(404);

        // 批量更新用户账户绑定
        group.MapPut("/", async (
                Guid userId,
                UpdateUserAccountBindingsRequest request,
                UserAccountBindingService bindingService) =>
            {
                try
                {
                    var result = await bindingService.UpdateUserAccountBindingsAsync(userId, request.AccountBindings);

                    return Results.Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "账户绑定更新成功"
                    });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"更新失败: {ex.Message}"
                    });
                }
            })
            .WithName("UpdateUserAccountBindings")
            .WithSummary("批量更新用户账户绑定")
            .Produces<ApiResponse<object>>()
            .Produces<ApiResponse<object>>(400);

        // 获取用户可见的所有账户（用于绑定选择）
        group.MapGet("/available-accounts", async (
                Guid userId,
                UserAccountBindingService bindingService,
                IUserContext userContext) =>
            {
                var isAdmin = userContext.IsAdmin();

                var accounts = await bindingService.GetVisibleAccountsForUserAsync(userId, isAdmin);

                return Results.Ok(new ApiResponse<List<Accounts>>
                {
                    Success = true,
                    Data = accounts,
                    Message = "获取可用账户列表成功"
                });
            })
            .WithName("GetAvailableAccountsForBinding")
            .WithSummary("获取用户可见的所有账户")
            .Produces<ApiResponse<List<Accounts>>>();

        // 检查用户是否可以访问指定账户
        group.MapGet("/check-access/{accountId}", async (
                Guid userId,
                string accountId,
                IUserContext userContext,
                UserAccountBindingService bindingService) =>
            {
                var isAdmin = userContext.IsAdmin();

                var canAccess = await bindingService.CanUserAccessAccountAsync(userId, accountId, isAdmin);

                return Results.Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new { CanAccess = canAccess },
                    Message = canAccess ? "用户可以访问该账户" : "用户无法访问该账户"
                });
            })
            .WithName("CheckUserAccountAccess")
            .WithSummary("检查用户是否可以访问指定账户")
            .Produces<ApiResponse<object>>();
    }
}