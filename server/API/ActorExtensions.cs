using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Service;

namespace API;

public static class ActorExtensions
{
    private const string StampClaim = "vsl:stamp";

    ///Builds the cookie identity and contains no password material, only the stamp
    public static ClaimsPrincipal ToPrincipal(this Actor actor)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, actor.Id.ToString()),
            new(ClaimTypes.Name, actor.Username),
            new(ClaimTypes.Role, actor.Role.ToString()),
            new(StampClaim, actor.Stamp)
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }

    public static Actor? TryGetActor(this ClaimsPrincipal? principal)
    {
        if (principal is null) return null;

        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = principal.FindFirstValue(ClaimTypes.Name);
        var role = principal.FindFirstValue(ClaimTypes.Role);
        var stamp = principal.FindFirstValue(StampClaim);

        if (!int.TryParse(id, out var userId) || name is null || stamp is null || !RolePolicy.TryParse(role, out var parsedRole))
            return null;

        return new Actor(userId, name, parsedRole, stamp);
    }

    public static Actor ToActor(this ClaimsPrincipal principal) =>
        principal.TryGetActor() ?? throw new InvalidOperationException("Authenticated principal is missing required claims.");
}
