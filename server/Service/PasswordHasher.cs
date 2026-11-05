using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace Service;

public interface IPasswordHasher
{
    ///Returns a self-describing hash string (algorithm, parameters, salt, hash)
    string Hash(string password);

    bool Verify(string password, string storedHash);

    /// Spends the same time as a real verification. Used when the username does not
    /// exist so that response time does not reveal which usernames are registered
    void VerifyAgainstDummy(string password);
}

public sealed class Argon2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int MemoryKiB = 19456;
    private const int Iterations = 2;
    private const int Parallelism = 1;

    private readonly string _dummyHash;

    public Argon2PasswordHasher()
    {
        _dummyHash = Hash(Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)));
    }

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Compute(password, salt, MemoryKiB, Iterations, Parallelism, HashSize);
        return $"$argon2id$v=19$m={MemoryKiB},t={Iterations},p={Parallelism}" +
               $"${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string storedHash)
    {
        try
        {
            // "", "argon2id", "v=19", "m=..,t=..,p=..", salt, hash
            var parts = storedHash.Split('$');
            if (parts.Length != 6 || parts[1] != "argon2id" || parts[2] != "v=19")
                return false;

            var settings = parts[3].Split(',');
            var memory = int.Parse(settings[0].AsSpan(2));
            var iterations = int.Parse(settings[1].AsSpan(2));
            var parallelism = int.Parse(settings[2].AsSpan(2));
            var salt = Convert.FromBase64String(parts[4]);
            var expected = Convert.FromBase64String(parts[5]);

            var actual = Compute(password, salt, memory, iterations, parallelism, expected.Length);

            // Constant-time comparison.
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (Exception ex) when (ex is FormatException or IndexOutOfRangeException or OverflowException or ArgumentException)
        {
            return false;
        }
    }

    public void VerifyAgainstDummy(string password) => Verify(password, _dummyHash);

    private static byte[] Compute(string password, byte[] salt, int memoryKiB, int iterations, int parallelism, int length)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = memoryKiB,
            Iterations = iterations,
            DegreeOfParallelism = parallelism
        };
        return argon2.GetBytes(length);
    }
}
