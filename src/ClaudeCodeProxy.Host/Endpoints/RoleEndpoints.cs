using ClaudeCodeProxy.Host.Models;
using ClaudeCodeProxy.Host.Services;

namespace ClaudeCodeProxy.Host.Endpoints;

/// <summary>
///     角色管理端点
/// </summary>
public static class RoleEndpoints
{
    public static void MapRoleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/roles")
            .WithTags("角色管理")
            .RequireAuthorization();

        // 获取所有角色
        group.MapGet("/", async (RoleService roleService) =>
            {
                var roles = await roleService.GetRolesAsync();
                return Results.Ok(new ApiResponse<List<RoleDto>>
                {
                    Success = true,
                    Data = roles,
                    Message = "获取角色列表成功"
                });
            })
            .WithName("GetRoles")
            .WithSummary("获取所有角色")
            .Produces<ApiResponse<List<RoleDto>>>();

        // 根据ID获取角色
        group.MapGet("/{id:int}", async (int id, RoleService roleService) =>
            {
                var role = await roleService.GetRoleByIdAsync(id);
                if (role == null)
                    return Results.NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "角色不存在"
                    });

                return Results.Ok(new ApiResponse<RoleDto>
                {
                    Success = true,
                    Data = role,
                    Message = "获取角色信息成功"
                });
            })
            .WithName("GetRoleById")
            .WithSummary("根据ID获取角色")
            .Produces<ApiResponse<RoleDto>>();

        // 创建角色
        group.MapPost("/", async (CreateRoleRequest request, RoleService roleService) =>
            {
                try
                {
                    var role = await roleService.CreateRoleAsync(request);
                    return Results.Created($"/api/roles/{role.Id}", new ApiResponse<RoleDto>
                    {
                        Success = true,
                        Data = role,
                        Message = "创建角色成功"
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
            .WithName("CreateRole")
            .WithSummary("创建角色")
            .Produces<ApiResponse<RoleDto>>(201)
            .Produces<ApiResponse<object>>(400);

        // 更新角色
        group.MapPut("/{id:int}", async (int id, UpdateRoleRequest request, RoleService roleService) =>
            {
                try
                {
                    var role = await roleService.UpdateRoleAsync(id, request);
                    if (role == null)
                        return Results.NotFound(new ApiResponse<object>
                        {
                            Success = false,
                            Message = "角色不存在"
                        });

                    return Results.Ok(new ApiResponse<RoleDto>
                    {
                        Success = true,
                        Data = role,
                        Message = "更新角色成功"
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
            .WithName("UpdateRole")
            .WithSummary("更新角色")
            .Produces<ApiResponse<RoleDto>>()
            .Produces<ApiResponse<object>>(400)
            .Produces<ApiResponse<object>>(404);

        // 删除角色
        group.MapDelete("/{id:int}", async (int id, RoleService roleService) =>
            {
                try
                {
                    var result = await roleService.DeleteRoleAsync(id);
                    if (!result)
                        return Results.NotFound(new ApiResponse<object>
                        {
                            Success = false,
                            Message = "角色不存在"
                        });

                    return Results.Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "删除角色成功"
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
            .WithName("DeleteRole")
            .WithSummary("删除角色")
            .Produces<ApiResponse<object>>()
            .Produces<ApiResponse<object>>(400)
            .Produces<ApiResponse<object>>(404);

        // 获取所有可用权限
        group.MapGet("/permissions", (RoleService roleService) =>
            {
                var permissions = roleService.GetAllPermissions();
                return Results.Ok(new ApiResponse<List<string>>
                {
                    Success = true,
                    Data = permissions,
                    Message = "获取权限列表成功"
                });
            })
            .WithName("GetPermissions")
            .WithSummary("获取所有可用权限")
            .Produces<ApiResponse<List<string>>>();

        // 获取用户权限
        group.MapGet("/user/{userId:Guid}/permissions", async (Guid userId, RoleService roleService) =>
            {
                var permissions = await roleService.GetUserPermissionsAsync(userId);
                return Results.Ok(new ApiResponse<List<string>>
                {
                    Success = true,
                    Data = permissions,
                    Message = "获取用户权限成功"
                });
            })
            .WithName("GetUserPermissions")
            .WithSummary("获取用户权限")
            .Produces<ApiResponse<List<string>>>();
    }
}