namespace EnterpriseFlow.Application.Abstractions;

/// <summary>Issues access tokens (JWT) and opaque refresh tokens (HU-002).</summary>
public interface ITokenService
{
    AccessToken GenerateAccessToken(Guid userId, Guid tenantId, IEnumerable<string> permissions);

    string GenerateRefreshToken();

    string HashRefreshToken(string rawToken);
}

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAtUtc);
