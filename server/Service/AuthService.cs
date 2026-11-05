using Infra;
using Infra.Entities;

namespace Service;

public sealed class AuthService(IUserRepository users, IPasswordHasher hasher)
{
    /// Returns the authenticated user, or null. The caller cannot tell whether the
    /// username or the password was wrong, both paths return null after similar work
    public async Task<Actor?> LoginAsync(string? username, string? password)
    {
        username = username?.Trim();
        password ??= "";

        // Bound the input before spending memory/CPU on hashing.
        if (string.IsNullOrEmpty(username) || username.Length > UserRules.MaxUsernameLength
            || password.Length > UserRules.MaxPasswordLength)
            return null;

        UserEntity? user = await users.FindByUsernameAsync(username);
        if (user is null)
        {
            hasher.VerifyAgainstDummy(password);
            return null;
        }

        return hasher.Verify(password, user.PasswordHash) ? Actor.From(user) : null;
    }
}
