using EnterpriseFlow.Application.Abstractions;
using MediatR;

namespace EnterpriseFlow.Application.Common.Behaviors;

public sealed class AuthorizationBehavior<TRequest, TResponse>(ICurrentUserService currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is IRequirePermission requiresPermission
            && !currentUser.HasPermission(requiresPermission.RequiredPermission))
        {
            throw new ForbiddenAccessException(requiresPermission.RequiredPermission);
        }

        return next();
    }
}
