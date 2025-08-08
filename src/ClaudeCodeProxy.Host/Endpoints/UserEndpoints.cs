using ClaudeCodeProxy.Host.Filters;
using ClaudeCodeProxy.Host.Models;
using ClaudeCodeProxy.Host.Services;
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
    }
}