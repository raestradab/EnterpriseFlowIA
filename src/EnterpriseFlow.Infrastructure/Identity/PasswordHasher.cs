using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace EnterpriseFlow.Infrastructure.Identity;

/// <summary>
/// Wraps ASP.NET Core Identity's <see cref="PasswordHasher{TUser}"/> (PBKDF2-HMACSHA256,
/// 100k iterations, versioned format) instead of hand-rolling password hashing — reusing a
/// battle-tested implementation is the correct call for anything security-critical, not a
/// place to demonstrate custom crypto code. <c>TUser</c> is unused by the default
/// implementation, so a null user is safe to pass.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _inner = new();

    public string Hash(string password) => _inner.HashPassword(null!, password);

    public bool Verify(string password, string passwordHash) =>
        _inner.VerifyHashedPassword(null!, passwordHash, password) != PasswordVerificationResult.Failed;
}
