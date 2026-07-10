namespace EnterpriseFlow.Application.Abstractions;

/// <summary>
/// Salted, slow hashing for user credentials (<see cref="Domain.Entities.User.PasswordHash"/>).
/// Deliberately separate from refresh-token hashing (<c>ITokenService.HashRefreshToken</c>):
/// a password is low-entropy and needs salting + deliberate slowness against brute force; a
/// refresh token is high-entropy random data looked up by exact match, where a fast
/// deterministic hash is correct and a slow salted one would just make every request slower
/// for no security benefit.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);
}
