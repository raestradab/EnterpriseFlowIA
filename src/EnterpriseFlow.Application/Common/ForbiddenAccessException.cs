namespace EnterpriseFlow.Application.Common;

/// <summary>
/// Raised by <see cref="Behaviors.AuthorizationBehavior{TRequest,TResponse}"/> when the
/// current user lacks the permission required by the request. Mapped to HTTP 403 by the
/// Api's exception-handling middleware.
/// </summary>
public sealed class ForbiddenAccessException(string permission)
    : Exception($"The current user does not have the required permission '{permission}'.")
{
}
