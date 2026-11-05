using System.Security.Cryptography;
using System.Text;
using Infra.Entities;

namespace Service;

/// The authenticated caller is a fingerprint of the stored
/// password hash: when the password changes, the stamp changes and
/// every session issued before the change stops being valid
public sealed record Actor(int Id, string Username, Role Role, string Stamp)
{
    public static Actor From(UserEntity user) => new(
        user.Id,
        user.Username,
        RolePolicy.TryParse(user.Role, out var role)
            ? role
            : throw new InvalidOperationException($"User {user.Id} has an invalid role."),
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(user.PasswordHash)))[..16]);
}
