namespace EnterpriseFlow.Application.Common;

/// <summary>
/// HU-001, security review finding: thrown when registration can't proceed for a reason that
/// must NOT be disclosed to an anonymous caller (currently: email already registered — telling
/// them so would let an attacker enumerate registered accounts across every tenant). Unlike the
/// tenant-slug-taken case (still reported as a specific field error — slugs are semi-public
/// organization identifiers, not personal data), this is deliberately generic.
/// </summary>
public sealed class RegistrationFailedException()
    : Exception("Unable to complete registration with the provided information.")
{
}
