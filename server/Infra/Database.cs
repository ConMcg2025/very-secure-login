using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;
using Microsoft.Data.Sqlite;

namespace Infra;

public static class Database
{
    public static DataOptions CreateOptions(string connectionString) =>
        new DataOptions().UseSQLite(connectionString, SQLiteProvider.Microsoft);

    ///Creates the database file/folder and the schema if they do not exist yet
    public static void EnsureCreated(string connectionString)
    {
        var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
        var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        using var db = new AppDb(CreateOptions(connectionString));

        // Username is unique and case-insensitive ("Hp" and "hp" are the same account).
        db.Execute(
            """
            CREATE TABLE IF NOT EXISTS Users (
                Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                Username     TEXT NOT NULL COLLATE NOCASE UNIQUE,
                PasswordHash TEXT NOT NULL,
                Role         TEXT NOT NULL CHECK (Role IN ('User', 'SuperUser', 'Admin')),
                CreatedAt    TEXT NOT NULL
            )
            """);
    }
}
