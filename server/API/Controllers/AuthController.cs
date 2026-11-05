using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Service;

namespace API.Controllers;

public sealed record LoginRequest(string? Username, string? Password);

/// <summary>What the client needs to know about the signed-in user (including what they may manage).</summary>
public sealed record MeResponse(int Id, string Username, string Role, string[] ManageableRoles)
{
    public static MeResponse From(Actor actor) => new(
        actor.Id,
        actor.Username,
        actor.Role.ToString(),
        RolePolicy.Manageable(actor.Role).Select(r => r.ToString()).ToArray());
}

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService auth) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<MeResponse>> Login(LoginRequest request)
    {
        var actor = await auth.LoginAsync(request.Username, request.Password);
        if (actor is null)
        {
            // One message for "unknown user" and "wrong password".
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = "Invalid username or password."
            });
        }

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            actor.ToPrincipal(),
            new AuthenticationProperties { IsPersistent = false });

        return MeResponse.From(actor);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult<MeResponse> Me() => MeResponse.From(User.ToActor());
}
