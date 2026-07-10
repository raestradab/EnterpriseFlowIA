namespace EnterpriseFlow.Application.Common;

/// <summary>
/// HU-002: the refresh token is missing, expired, revoked, or already used (reuse detection —
/// see the sequence diagram in docs/03-diseno-arquitectura/04-secuencias.md). Mapped to 401,
/// forcing the client to re-authenticate with credentials.
/// </summary>
public sealed class InvalidRefreshTokenException() : Exception("The refresh token is invalid or has expired.")
{
}
