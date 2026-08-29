using System.Security.Claims;
using Example.Application.Interfaces.Identity;

namespace Example.Api.Authentication;

public static class ClaimsPrincipalExtensions
{
    public static string? GetUserId(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue("oid") ?? principal.FindFirstValue("sub");
    }

    public static string? GetEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue("email") ?? principal.FindFirstValue("preferred_username");
    }

    public static IReadOnlyCollection<string> GetRoles(this ClaimsPrincipal principal)
    {
        return principal.FindAll("roles").Select(claim => claim.Value).ToArray();
    }

    public static IReadOnlyCollection<string> GetGroups(this ClaimsPrincipal principal)
    {
        return principal.FindAll("groups")
            .Concat(principal.FindAll("cognito:groups"))
            .Select(claim => claim.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public static bool HasScope(this ClaimsPrincipal principal, string requiredScope)
    {
        return principal.FindAll("scope")
            .Concat(principal.FindAll("scp"))
            .SelectMany(claim => claim.Value.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Contains(requiredScope, StringComparer.Ordinal);
    }

    public static bool HasPermission(this ClaimsPrincipal principal, string requiredPermission)
    {
        return principal.HasScope(requiredPermission) ||
               principal.FindAll("roles")
                   .Concat(principal.FindAll("permissions"))
                   .Any(claim => string.Equals(
                       claim.Value,
                       requiredPermission,
                       StringComparison.Ordinal));
    }
}

internal sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public string? UserId => Principal?.GetUserId();

    public string? Email => Principal?.GetEmail();

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
}
