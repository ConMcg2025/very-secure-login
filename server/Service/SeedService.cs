using Infra;
using Infra.Entities;

namespace Service;

/// Creates the initial accounts on first start
/// Passwords are hashed before they are stored; the plaintext below exists only in this
/// source file as demo data and will not be used in a real deployment
public sealed class SeedService(IUserRepository users, IPasswordHasher hasher)
{
    private static readonly (string Username, string Password, Role Role)[] Accounts =
    [
        ("hp", "harry", Role.User),
        ("morgan", "freeman", Role.User),
        ("mj", "heehee", Role.SuperUser),
        ("creepyneighbor", "watchingu", Role.SuperUser),
        ("god", "jesus", Role.Admin)
    ];

    public async Task SeedAsync()
    {
        if (await users.AnyAsync())
            return;

        foreach (var (username, password, role) in Accounts)
        {
            await users.InsertAsync(new UserEntity
            {
                Username = username,
                PasswordHash = hasher.Hash(password),
                Role = role.ToString(),
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}
