using ClaudeCodeProxy.Host.Filters;
using ClaudeCodeProxy.Host.Models;
using ClaudeCodeProxy.Host.Services;
using ClaudeCodeProxy.Domain;
using Microsoft.AspNetCore.Authorization;

namespace ClaudeCodeProxy.Host.Endpoints;

/// <summary>
/// 用户管理端点
/// </summary>
public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("用户管理")
            .AddEndpointFilter<GlobalResponseFilter>()
            .RequireAuthorization();

        // 获取所有用户（分页）
        group.MapGet("/", async (UserService userService, int pageIndex = 1, int pageSize = 20) =>
            {
                var users = await userService.GetUsersAsync(pageIndex, pageSize);
                return users;
            })
            .WithName("GetUsers")
            .WithSummary("获取所有用户（分页）")
            .Produces<ApiResponse<PagedResult<UserDto>>>();

        // 根据ID获取用户
        group.MapGet("/{id:Guid}", async (Guid id, UserService userService) =>
            {
                var user = await userService.GetUserByIdAsync(id);
                return user;
            })
            .WithName("GetUserById")
            .WithSummary("根据ID获取用户")
            .Produces<ApiResponse<UserDto>>();

        // 创建用户
        group.MapPost("/", async (CreateUserRequest request, UserService userService) =>
            {
                var user = await userService.CreateUserAsync(request);
                return user;
            })
            .WithName("CreateUser")
            .WithSummary("创建用户")
            .Produces<ApiResponse<UserDto>>(201)
            .Produces<ApiResponse<object>>(400);

        // 更新用户
        group.MapPut("/{id:Guid}", async (Guid id, UpdateUserRequest request, UserService userService) =>
            {
                var user = await userService.UpdateUserAsync(id, request);
                return user;
            })
            .WithName("UpdateUser")
            .WithSummary("更新用户")
            .Produces<ApiResponse<UserDto>>()
            .Produces<ApiResponse<object>>(400)
            .Produces<ApiResponse<object>>(404);

        // 删除用户
        group.MapDelete("/{id:int}", async (int id, UserService userService) =>
            {
                var result = await userService.DeleteUserAsync(id);
                return true;
            })
            .WithName("DeleteUser")
            .WithSummary("删除用户")
            .Produces<ApiResponse<object>>()
            .Produces<ApiResponse<object>>(400)
            .Produces<ApiResponse<object>>(404);

        // 修改密码
        group.MapPut("/{id:Guid}/password", async (Guid id, ChangePasswordRequest request, UserService userService) =>
            {
                var result = await userService.ChangePasswordAsync(id, request);
                return true;
            })
            .WithName("ChangePassword")
            .WithSummary("修改密码")
            .Produces<ApiResponse<object>>()
            .Produces<ApiResponse<object>>(400)
            .Produces<ApiResponse<object>>(404);

        // 重置密码（管理员功能）
        group.MapPut("/{id:int}/reset-password",
                async (int id, ResetPasswordRequest request, UserService userService) =>
                {
                    try
                    {
                        var result = await userService.ResetPasswordAsync(id, request);
                        if (!result)
                        {
                            return Results.NotFound(new ApiResponse<object>
                            {
                                Success = false,
                                Message = "用户不存在"
                            });
                        }

                        return Results.Ok(new ApiResponse<object>
                        {
                            Success = true,
                            Message = "重置密码成功"
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
                })
            .WithName("ResetPassword")
            .WithSummary("重置密码")
            .Produces<ApiResponse<object>>()
            .Produces<ApiResponse<object>>(400)
            .Produces<ApiResponse<object>>(404);

        // 获取用户登录历史
        group.MapGet("/{id:Guid}/login-history",
                async (Guid id, UserService userService, int pageIndex = 0, int pageSize = 20) =>
                {
                    var history = await userService.GetUserLoginHistoryAsync(id, pageIndex, pageSize);
                    return Results.Ok(new ApiResponse<List<UserLoginHistoryDto>>
                    {
                        Success = true,
                        Data = history,
                        Message = "获取登录历史成功"
                    });
                })
            .WithName("GetUserLoginHistory")
            .WithSummary("获取用户登录历史")
            .Produces<ApiResponse<List<UserLoginHistoryDto>>>();

        // 获取用户账户绑定管理信息
        group.MapGet("/{id:Guid}/account-bindings", 
                async (Guid id, UserAccountBindingService bindingService) =>
                {
                    try
                    {
                        var managementDto = await bindingService.GetAccountBindingManagementAsync(id);
                        return Results.Ok(new ApiResponse<AccountBindingManagementDto>
                        {
                            Success = true,
                            Data = managementDto,
                            Message = "获取用户账户绑定信息成功"
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
                })
            .WithName("GetUserAccountBindingManagement")
            .WithSummary("获取用户账户绑定管理信息")
            .Produces<ApiResponse<AccountBindingManagementDto>>()
            .Produces<ApiResponse<object>>(400);

        // 绑定用户到账户
        group.MapPost("/{id:Guid}/account-bindings", 
                async (Guid id, BindAccountRequest request, UserAccountBindingService bindingService) =>
                {
                    try
                    {
                        var binding = await bindingService.BindUserToAccountAsync(
                            id, 
                            request.AccountId, 
                            request.Priority, 
                            request.BindingType, 
                            request.Remarks);
                        
                        return Results.Ok(new ApiResponse<UserAccountBinding>
                        {
                            Success = true,
                            Data = binding,
                            Message = "绑定账户成功"
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
                        return Results.BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = ex.Message
                        });
                    }
                })
            .WithName("CreateUserAccountBinding")
            .WithSummary("绑定用户到账户")
            .Produces<ApiResponse<UserAccountBinding>>(201)
            .Produces<ApiResponse<object>>(400);

        // 解除用户账户绑定
        group.MapDelete("/{id:Guid}/account-bindings/{accountId}", 
                async (Guid id, string accountId, UserAccountBindingService bindingService) =>
                {
                    try
                    {
                        var result = await bindingService.UnbindUserFromAccountAsync(id, accountId);
                        if (!result)
                        {
                            return Results.NotFound(new ApiResponse<object>
                            {
                                Success = false,
                                Message = "找不到该绑定关系"
                            });
                        }

                        return Results.Ok(new ApiResponse<object>
                        {
                            Success = true,
                            Message = "解除绑定成功"
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
                })
            .WithName("UnbindUserAccountBinding")
            .WithSummary("解除用户账户绑定")
            .Produces<ApiResponse<object>>()
            .Produces<ApiResponse<object>>(400)
            .Produces<ApiResponse<object>>(404);

        // 更新用户账户绑定优先级
        group.MapPut("/{id:Guid}/account-bindings/{accountId}/priority", 
                async (Guid id, string accountId, UpdateBindingPriorityRequest request, UserAccountBindingService bindingService) =>
                {
                    try
                    {
                        var result = await bindingService.UpdateBindingPriorityAsync(id, accountId, request.Priority);
                        if (!result)
                        {
                            return Results.NotFound(new ApiResponse<object>
                            {
                                Success = false,
                                Message = "找不到该绑定关系"
                            });
                        }

                        return Results.Ok(new ApiResponse<object>
                        {
                            Success = true,
                            Message = "更新优先级成功"
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
                })
            .WithName("UpdateUserBindingPriority")
            .WithSummary("更新用户账户绑定优先级")
            .Produces<ApiResponse<object>>()
            .Produces<ApiResponse<object>>(400)
            .Produces<ApiResponse<object>>(404);

        // 批量更新用户账户绑定
        group.MapPut("/{id:Guid}/account-bindings", 
                async (Guid id, UpdateUserAccountBindingsRequest request, UserAccountBindingService bindingService) =>
                {
                    try
                    {
                        var result = await bindingService.UpdateUserAccountBindingsAsync(id, request.AccountBindings);
                        return Results.Ok(new ApiResponse<object>
                        {
                            Success = true,
                            Message = "批量更新绑定成功"
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
                })
            .WithName("BatchUpdateUserAccountBindings")
            .WithSummary("批量更新用户账户绑定")
            .Produces<ApiResponse<object>>()
            .Produces<ApiResponse<object>>(400);
    }
}