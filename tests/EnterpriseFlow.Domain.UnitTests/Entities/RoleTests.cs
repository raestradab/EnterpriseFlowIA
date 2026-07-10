using EnterpriseFlow.Domain.Entities;
using EnterpriseFlow.Domain.Exceptions;
using FluentAssertions;

namespace EnterpriseFlow.Domain.UnitTests.Entities;

public class RoleTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_With_Missing_Name_Throws(string name)
    {
        var act = () => Role.Create(name);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Trims_The_Name()
    {
        var role = Role.Create("  Administrator  ");

        role.Name.Should().Be("Administrator");
    }

    [Fact]
    public void AssignTenant_Sets_The_TenantId()
    {
        var role = Role.Create("Administrator");
        var tenantId = Guid.NewGuid();

        role.AssignTenant(tenantId);

        role.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void MarkDeleted_Sets_IsDeleted_And_DeletedAtUtc()
    {
        var role = Role.Create("Administrator");

        role.MarkDeleted();

        role.IsDeleted.Should().BeTrue();
        role.DeletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void GrantPermission_Adds_It()
    {
        var role = Role.Create("Administrator");

        role.GrantPermission("companies.manage");

        role.HasPermission("companies.manage").Should().BeTrue();
    }

    [Fact]
    public void GrantPermission_Same_Permission_Twice_Throws()
    {
        var role = Role.Create("Administrator");
        role.GrantPermission("companies.manage");

        var act = () => role.GrantPermission("companies.manage");

        act.Should().Throw<RoleAlreadyHasPermissionException>();
    }

    [Fact]
    public void RevokePermission_Removes_It()
    {
        var role = Role.Create("Administrator");
        role.GrantPermission("companies.manage");

        role.RevokePermission("companies.manage");

        role.HasPermission("companies.manage").Should().BeFalse();
    }
}
