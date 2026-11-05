using LinqToDB.Mapping;

namespace Infra.Entities;

/// <summary>
/// Database row for a user. <see cref="PasswordHash"/> only ever holds an
/// Argon2id hash string - the plaintext password is never persisted.
/// <see cref="Role"/> is stored as text: "User", "SuperUser" or "Admin".
/// </summary>
[Table("Users")]
public class UserEntity
{
    [PrimaryKey, Identity]
    public int Id { get; set; }

    [Column(Length = 64), NotNull]
    public string Username { get; set; } = "";

    [Column, NotNull]
    public string PasswordHash { get; set; } = "";

    [Column(Length = 16), NotNull]
    public string Role { get; set; } = "";

    [Column, NotNull]
    public DateTime CreatedAt { get; set; }
}
