using Infra.Entities;
using LinqToDB;
using Microsoft.Data.Sqlite;

namespace Infra;

public class DuplicateUsernameException(string username)
    : Exception($"Username '{username}' is already taken.");

public interface IUserRepository
{
    Task<List<UserEntity>> ListAsync(List<string> roles);
    Task<UserEntity?> FindByIdAsync(int id);
    Task<UserEntity?> FindByUsernameAsync(string username);
    Task<bool> AnyAsync();
    Task<int> InsertAsync(UserEntity user);
    Task UpdateAsync(UserEntity user);
    Task DeleteAsync(int id);
}

public class UserRepository(AppDb db) : IUserRepository
{
    private const int SqliteConstraint = 19;

    public Task<List<UserEntity>> ListAsync(List<string> roles) =>
        Task.FromResult(
            db.Users
                .Where(u => roles.Contains(u.Role))
                .OrderBy(u => u.Username)
                .ToList());

    public Task<UserEntity?> FindByIdAsync(int id) =>
        Task.FromResult(db.Users.FirstOrDefault(u => u.Id == id));

    // The column uses COLLATE NOCASE, so this comparison is case-insensitive.
    public Task<UserEntity?> FindByUsernameAsync(string username) =>
        Task.FromResult(db.Users.FirstOrDefault(u => u.Username == username));

    public Task<bool> AnyAsync() => Task.FromResult(db.Users.Any());

    public async Task<int> InsertAsync(UserEntity user)
    {
        try
        {
            return await db.InsertWithInt32IdentityAsync(user);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == SqliteConstraint)
        {
            throw new DuplicateUsernameException(user.Username);
        }
    }

    public async Task UpdateAsync(UserEntity user)
    {
        try
        {
            await db.UpdateAsync(user);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == SqliteConstraint)
        {
            throw new DuplicateUsernameException(user.Username);
        }
    }

    public async Task DeleteAsync(int id) =>
        await db.Users.Where(u => u.Id == id).DeleteAsync();
}