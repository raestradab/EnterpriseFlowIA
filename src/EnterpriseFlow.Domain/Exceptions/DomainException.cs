namespace EnterpriseFlow.Domain.Exceptions;

/// <summary>
/// Base type for violations of a domain invariant (e.g. HU-021: closing a project with open
/// tasks). Distinct from validation errors (FluentValidation, Application layer, input shape)
/// — this represents a business rule broken on a valid, well-formed input.
/// </summary>
public abstract class DomainException(string message) : Exception(message)
{
}
