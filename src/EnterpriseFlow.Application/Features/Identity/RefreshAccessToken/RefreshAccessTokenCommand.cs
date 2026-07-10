using EnterpriseFlow.Application.Features.Identity.Login;
using MediatR;

namespace EnterpriseFlow.Application.Features.Identity.RefreshAccessToken;

public sealed record RefreshAccessTokenCommand(string RefreshToken) : IRequest<LoginResult>;
