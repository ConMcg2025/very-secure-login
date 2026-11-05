using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service;

namespace API.Controllers;

/// <summary>
/// Requires a signed-in user. Which operations a given role may perform is decided
/// in <see cref="UserService"/> (see <see cref="RolePolicy"/>), because it depends on
/// the role of the account being touched, not only on the caller.
/// </summary>
[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController(UserService users) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserView>>> List() =>
        Ok(await users.ListAsync(User.ToActor()));

    [HttpPost]
    public async Task<ActionResult<UserView>> Create(CreateUserRequest request)
    {
        var created = await users.CreateAsync(User.ToActor(), request);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserView>> Update(int id, UpdateUserRequest request) =>
        Ok(await users.UpdateAsync(User.ToActor(), id, request));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await users.DeleteAsync(User.ToActor(), id);
        return NoContent();
    }
}
