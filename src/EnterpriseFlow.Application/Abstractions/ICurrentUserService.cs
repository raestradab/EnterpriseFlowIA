namespace EnterpriseFlow.Application.Abstractions;

/// <summary>
/// Resolves the authenticated user of the current request, including the flattened set of
/// permission claims used by <c>AuthorizationBehavior</c> (ADR-0004).
/// </summary>
public interface ICurrentUserService
{
    Guid UserId { get; }

    IReadOnlyCollection<string> Permissions { get; }

    bool HasPermission(string permission);
}
