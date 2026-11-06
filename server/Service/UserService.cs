using System.Text.RegularExpressions;
using Infra;
using Infra.Entities;

namespace Service;

public sealed record UserView(int Id, string Username, string Role, DateTime CreatedAt);

public sealed record CreateUserRequest(string? Username, string? Password, string? Role);

public sealed record UpdateUserRequest(string? Username, string? Password, string? Role);

public static partial class UserRules
{
    public const int MinUsernameLength = 3;
    public const int MaxUsernameLength = 32;
    public const int MinPasswordLength = 3;
    public const int MaxPasswordLength = 128;

    [GeneratedRegex("^[A-Za-z0-9_.-]+$")]
    private static partial Regex UsernamePattern();

    public static string ValidateUsername(string? username)
    {
        username = username?.Trim();
        if (string.IsNullOrEmpty(username) || username.Length < MinUsernameLength || username.Length > MaxUsernameLength)
            throw new ValidationFailedException(
                $"Username must be {MinUsernameLength}-{MaxUsernameLength} characters.");
        if (!UsernamePattern().IsMatch(username))
            throw new ValidationFailedException(
                "Username may only contain letters, digits, '.', '_' and '-'.");
        return username;
    }

    public static string ValidatePassword(string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinPasswordLength || password.Length > MaxPasswordLength)
            throw new ValidationFailedException(
                $"Password must be {MinPasswordLength}-{MaxPasswordLength} characters.");
        return password;
    }

    public static Role ParseRole(string? role) =>
        RolePolicy.TryParse(role, out var parsed)
            ? parsed
            : throw new ValidationFailedException("Role must be one of: User, SuperUser, Admin.");
}

public sealed class UserService(IUserRepository users, IPasswordHasher hasher)
{
    public async Task<IReadOnlyList<UserView>> ListAsync(Actor actor)
    {
        var roles = RolePolicy.Readable(actor.Role).Select(r => r.ToString()).ToList();
        var rows = await users.ListAsync(roles);
        return rows.Select(ToView).ToList();
    }

    public async Task<UserView> CreateAsync(Actor actor, CreateUserRequest request)
    {
        var username = UserRules.ValidateUsername(request.Username);
        var password = UserRules.ValidatePassword(request.Password);
        var role = UserRules.ParseRole(request.Role);

        if (!RolePolicy.CanManage(actor.Role, role))
            throw new ForbiddenException($"You are not allowed to create accounts with the role {role}.");

        if (await users.FindByUsernameAsync(username) is not null)
            throw new ConflictException($"Username '{username}' is already taken.");

        var entity = new UserEntity
        {
            Username = username,
            PasswordHash = hasher.Hash(password),
            Role = role.ToString(),
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            entity.Id = await users.InsertAsync(entity);
        }
        catch (DuplicateUsernameException)
        {
            throw new ConflictException($"Username '{username}' is already taken.");
        }

        return ToView(entity);
    }

    public async Task<UserView> UpdateAsync(Actor actor, int id, UpdateUserRequest request)
    {
        var target = await GetManageableAsync(actor, id);
        var currentRole = UserRules.ParseRole(target.Role);

        if (request.Role is not null)
        {
            var newRole = UserRules.ParseRole(request.Role);

            // Stops a SuperUser promoting someone to Admin, and anyone changing their own role.
            if (!RolePolicy.CanManage(actor.Role, newRole))
                throw new ForbiddenException($"You are not allowed to assign the role {newRole}.");
            if (target.Id == actor.Id && newRole != currentRole)
                throw new ForbiddenException("You cannot change your own role.");

            target.Role = newRole.ToString();
        }

        if (request.Username is not null)
        {
            var username = UserRules.ValidateUsername(request.Username);
            var existing = await users.FindByUsernameAsync(username);
            if (existing is not null && existing.Id != target.Id)
                throw new ConflictException($"Username '{username}' is already taken.");
            target.Username = username;
        }

        if (!string.IsNullOrEmpty(request.Password))
            target.PasswordHash = hasher.Hash(UserRules.ValidatePassword(request.Password));

        try
        {
            await users.UpdateAsync(target);
        }
        catch (DuplicateUsernameException)
        {
            throw new ConflictException($"Username '{target.Username}' is already taken.");
        }

        return ToView(target);
    }

    public async Task DeleteAsync(Actor actor, int id)
    {
        var target = await GetManageableAsync(actor, id);

        if (target.Id == actor.Id)
            throw new ForbiddenException("You cannot delete your own account.");

        await users.DeleteAsync(target.Id);
    }

    private async Task<UserEntity> GetManageableAsync(Actor actor, int id)
    {
        var target = await users.FindByIdAsync(id);
        if (target is null || !RolePolicy.TryParse(target.Role, out var role) || !RolePolicy.CanRead(actor.Role, role))
            throw new NotFoundException("User not found.");

        if (!RolePolicy.CanManage(actor.Role, role))
            throw new ForbiddenException($"You are not allowed to modify accounts with the role {role}.");

        return target;
    }

    // The view never contains the password hash.
    private static UserView ToView(UserEntity u) =>
        new(u.Id, u.Username, u.Role, DateTime.SpecifyKind(u.CreatedAt, DateTimeKind.Utc));
}
