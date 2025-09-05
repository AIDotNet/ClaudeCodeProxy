using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace ClaudeCodeProxy.Core;

public class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public Guid? GetCurrentUserId()
    {
        var user = GetCurrentUser();
        var value = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (Guid.TryParse(value, out var userId)) return userId;

        return null;
    }

    public ClaimsPrincipal? GetCurrentUser()
    {
        return httpContextAccessor.HttpContext?.User;
    }

    public bool IsAuthenticated()
    {
        var user = GetCurrentUser();
        return user?.Identity?.IsAuthenticated ?? false;
    }

    public string? GetCurrentUserRole()
    {
        var user = GetCurrentUser();
        return user?.FindFirst(ClaimTypes.Role)?.Value;
    }

    public bool IsAdmin()
    {
        var user = GetCurrentUser();
        return user?.IsInRole("Admin") ?? false;
    }
}