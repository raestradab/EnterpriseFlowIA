using EnterpriseFlow.Domain.Common;

namespace EnterpriseFlow.Domain.Entities;

/// <summary>
/// F1.1 (Registro de Tenant). Deliberately NOT <see cref="ITenantScoped"/> — a Tenant defines
/// tenant boundaries, it isn't scoped by one. Also skips <see cref="IAuditableEntity"/>: it is
/// the one entity created before any authenticated user/tenant context exists (registration is
/// anonymous), so "who created it" doesn't have the usual meaning.
/// </summary>
public sealed class Tenant : BaseEntity
{
    private Tenant()
    {
        Name = string.Empty;
        Slug = string.Empty;
    }

    public string Name { get; private set; }

    public string Slug { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static Tenant Create(string name, string slug)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Tenant name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException("Tenant slug is required.", nameof(slug));
        }

        return new Tenant
        {
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }
}
