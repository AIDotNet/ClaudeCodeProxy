using System.Security.Claims;
using ClaudeCodeProxy.Core;
using ClaudeCodeProxy.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClaudeCodeProxy.Host.Services;

/// <summary>
///     当前用户服务，用于获取当前登录用户信息
/// </summary>
public class CurrentUserService(IHttpContextAccessor httpContextAccessor, IContext context)
{
    /// <summary>
    ///     获取当前用户ID
    /// </summary>
    /// <returns>用户ID，如果未登录返回null</returns>
    public Guid? GetCurrentUserId()
    {
        var userIdClaim = httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim)) return null;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    ///     获取当前用户是否为管理员
    /// </summary>
    /// <returns>是否为管理员</returns>
    public async Task<bool> IsCurrentUserAdminAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return false;

        var user = await context.Users
            .Include(u => u.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId.Value);

        return user?.Role?.Name?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    ///     获取当前用户信息
    /// </summary>
    /// <returns>当前用户信息</returns>
    public async Task<User?> GetCurrentUserAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return null;

        return await context.Users
            .Include(u => u.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId.Value);
    }

    /// <summary>
    ///     检查当前用户是否有指定权限
    /// </summary>
    /// <param name="permission">权限名称</param>
    /// <returns>是否有权限</returns>
    public bool HasPermission(string permission)
    {
        return httpContextAccessor.HttpContext?.User?.HasClaim("permission", permission) == true;
    }

    /// <summary>
    ///     检查当前用户是否可以访问指定用户的资源
    /// </summary>
    /// <param name="targetUserId">目标用户ID</param>
    /// <returns>是否可以访问</returns>
    public async Task<bool> CanAccessUserResourceAsync(Guid targetUserId)
    {
        var currentUserId = GetCurrentUserId();

        // 未登录用户不能访问任何资源
        if (currentUserId == null) return false;

        // 用户可以访问自己的资源
        if (currentUserId == targetUserId) return true;

        // 管理员可以访问所有用户的资源
        return await IsCurrentUserAdminAsync();
    }
}