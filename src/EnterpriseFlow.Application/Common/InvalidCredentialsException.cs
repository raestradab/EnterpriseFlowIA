namespace EnterpriseFlow.Application.Common;

/// <summary>HU-002: wrong email/password on login. Mapped to HTTP 401.</summary>
public sealed class InvalidCredentialsException() : Exception("Invalid email or password.")
{
}
