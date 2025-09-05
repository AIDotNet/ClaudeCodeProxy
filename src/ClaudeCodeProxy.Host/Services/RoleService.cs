using ClaudeCodeProxy.Core;
using ClaudeCodeProxy.Domain;
using ClaudeCodeProxy.Host.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaudeCodeProxy.Host.Services;

/// <summary>
///     角色服务
/// </summary>
public class RoleService(IContext context)
{
    /// <summary>
    ///     获取所有角色
    /// </summary>
    public async Task<List<RoleDto>> GetRolesAsync()
    {
        var roles = await context.Roles.ToListAsync();
        return roles.Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsSystem = r.IsSystem,
                Permissions = r.Permissions,
                UserCount = context.Users.Count(u => u.RoleId == r.Id && u.IsActive),
                CreatedAt = r.CreatedAt,
                ModifiedAt = r.ModifiedAt
            })
            .OrderBy(r => r.Id)
            .ToList();
    }

    /// <summary>
    ///     根据ID获取角色
    /// </summary>
    public async Task<RoleDto?> GetRoleByIdAsync(int id)
    {
        var role = await context.Roles.FirstOrDefaultAsync(r => r.Id == id);
        if (role == null) return null;

        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsSystem = role.IsSystem,
            Permissions = role.Permissions,
            UserCount = context.Users.Count(u => u.RoleId == role.Id && u.IsActive),
            CreatedAt = role.CreatedAt,
            ModifiedAt = role.ModifiedAt
        };
    }

    /// <summary>
    ///     创建角色
    /// </summary>
    public async Task<RoleDto> CreateRoleAsync(CreateRoleRequest request)
    {
        // 检查角色名是否已存在
        var existingRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == request.Name);
        if (existingRole != null) throw new ArgumentException("角色名已存在");

        var role = new Role
        {
            Name = request.Name,
            Description = request.Description,
            IsSystem = false,
            Permissions = request.Permissions
        };

        context.Roles.Add(role);
        await context.SaveAsync();

        return (await GetRoleByIdAsync(role.Id))!;
    }

    /// <summary>
    ///     更新角色
    /// </summary>
    public async Task<RoleDto?> UpdateRoleAsync(int id, UpdateRoleRequest request)
    {
        var role = await context.Roles.FindAsync(id);
        if (role == null) return null;

        // 系统角色不能修改
        if (role.IsSystem) throw new ArgumentException("系统角色不能修改");

        // 检查角色名是否已被其他角色使用
        var existingRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == request.Name && r.Id != id);
        if (existingRole != null) throw new ArgumentException("角色名已存在");

        role.Name = request.Name;
        role.Description = request.Description;
        role.Permissions = request.Permissions;

        await context.SaveAsync();

        return await GetRoleByIdAsync(id);
    }

    /// <summary>
    ///     删除角色
    /// </summary>
    public async Task<bool> DeleteRoleAsync(int id)
    {
        var role = await context.Roles.Include(r => r.Users).FirstOrDefaultAsync(r => r.Id == id);
        if (role == null) return false;

        // 系统角色不能删除
        if (role.IsSystem) throw new ArgumentException("系统角色不能删除");

        // 有用户的角色不能删除
        if (role.Users.Any()) throw new ArgumentException("该角色下还有用户，不能删除");

        context.Roles.Remove(role);
        await context.SaveAsync();

        return true;
    }

    /// <summary>
    ///     获取所有可用权限
    /// </summary>
    public List<string> GetAllPermissions()
    {
        return Permissions.GetAllPermissions();
    }

    /// <summary>
    ///     检查用户是否有指定权限
    /// </summary>
    public async Task<bool> HasPermissionAsync(Guid userId, string permission)
    {
        var user = await context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

        if (user == null) return false;

        var permissions = user.Role.Permissions;
        return permissions.Contains(permission);
    }

    /// <summary>
    ///     获取用户权限列表
    /// </summary>
    public async Task<List<string>> GetUserPermissionsAsync(Guid userId)
    {
        var user = await context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

        if (user == null) return new List<string>();

        return user.Role.Permissions;
    }

    /// <summary>
    ///     初始化默认角色
    /// </summary>
    public async Task InitializeDefaultRolesAsync()
    {
        // 创建管理员角色
        var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
        if (adminRole == null)
        {
            adminRole = new Role
            {
                Name = "Admin",
                Description = "系统管理员",
                IsSystem = true,
                Permissions = Permissions.GetAdminPermissions()
            };
            context.Roles.Add(adminRole);
        }

        // 创建普通用户角色
        var userRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
        if (userRole == null)
        {
            userRole = new Role
            {
                Name = "User",
                Description = "普通用户",
                IsSystem = true,
                Permissions = Permissions.GetUserPermissions()
            };
            context.Roles.Add(userRole);
        }

        await context.SaveAsync();
    }
}