using MediatR;

namespace EnterpriseFlow.Application.Features.Identity.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<LoginResult>;

public sealed record LoginResult(string AccessToken, DateTimeOffset AccessTokenExpiresAtUtc, string RefreshToken);
