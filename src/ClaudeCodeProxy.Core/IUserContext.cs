using System.Security.Claims;

namespace ClaudeCodeProxy.Core;

public interface IUserContext
{
    Guid? GetCurrentUserId();

    ClaimsPrincipal? GetCurrentUser();
    bool IsAuthenticated();
    string? GetCurrentUserRole();

    bool IsAdmin();
}