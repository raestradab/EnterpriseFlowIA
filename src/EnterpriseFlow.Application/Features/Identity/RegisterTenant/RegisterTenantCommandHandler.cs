using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Domain.Entities;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Identity.RegisterTenant;

public sealed class RegisterTenantCommandHandler(IAppDbContext db, IPasswordHasher passwordHasher)
    : IRequestHandler<RegisterTenantCommand, RegisterTenantResult>
{
    public async Task<RegisterTenantResult> Handle(RegisterTenantCommand request, CancellationToken cancellationToken)
    {
        var slug = request.TenantSlug.Trim().ToLowerInvariant();
        var email = request.AdminEmail.Trim().ToLowerInvariant();

        if (await db.Tenants.AnyAsync(t => t.Slug == slug, cancellationToken))
        {
            throw new ValidationException(
                [new ValidationFailure(nameof(request.TenantSlug), "This slug is already taken.")]);
        }

        // Global (cross-tenant) uniqueness check: bypasses the tenant filter deliberately —
        // there is no "current tenant" yet, and email must be unique platform-wide so login
        // can resolve a user from just an email (see LoginCommandHandler). The failure is
        // intentionally generic (RegistrationFailedException), not a field-specific "this email
        // is already registered" — security review finding: that message lets an anonymous
        // caller enumerate registered accounts across every tenant.
        if (await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email, cancellationToken))
        {
            throw new RegistrationFailedException();
        }

        var tenant = Tenant.Create(request.TenantName, slug);
        db.Tenants.Add(tenant);

        var adminRole = Role.Create("Administrator");
        adminRole.AssignTenant(tenant.Id);
        foreach (var permission in Permissions.All())
        {
            adminRole.GrantPermission(permission);
        }

        db.Roles.Add(adminRole);

        var admin = User.Create(email, passwordHasher.Hash(request.AdminPassword));
        admin.AssignTenant(tenant.Id);
        admin.AssignRole(adminRole.Id);
        db.Users.Add(admin);

        await db.SaveChangesAsync(cancellationToken);

        return new RegisterTenantResult(tenant.Id, admin.Id);
    }
}
