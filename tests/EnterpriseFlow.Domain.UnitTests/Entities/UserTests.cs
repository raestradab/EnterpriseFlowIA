using EnterpriseFlow.Domain.Entities;
using EnterpriseFlow.Domain.Exceptions;
using FluentAssertions;

namespace EnterpriseFlow.Domain.UnitTests.Entities;

public class UserTests
{
    [Fact]
    public void Create_With_Valid_Data_Normalizes_Email()
    {
        var user = User.Create("  Jane@ACME.com  ", "hashed-value");

        user.Email.Should().Be("jane@acme.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_With_Missing_Email_Throws(string email)
    {
        var act = () => User.Create(email, "hashed-value");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_With_Missing_PasswordHash_Throws(string passwordHash)
    {
        var act = () => User.Create("jane@acme.com", passwordHash);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AssignTenant_Sets_The_TenantId()
    {
        var user = User.Create("jane@acme.com", "hashed-value");
        var tenantId = Guid.NewGuid();

        user.AssignTenant(tenantId);

        user.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void MarkDeleted_Sets_IsDeleted_And_DeletedAtUtc()
    {
        var user = User.Create("jane@acme.com", "hashed-value");

        user.MarkDeleted();

        user.IsDeleted.Should().BeTrue();
        user.DeletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void AssignRole_Same_Role_Twice_Throws()
    {
        var user = User.Create("jane@acme.com", "hashed-value");
        var roleId = Guid.NewGuid();
        user.AssignRole(roleId);

        var act = () => user.AssignRole(roleId);

        act.Should().Throw<UserAlreadyHasRoleException>();
    }

    [Fact]
    public void RemoveRole_Removes_The_Assignment()
    {
        var user = User.Create("jane@acme.com", "hashed-value");
        var roleId = Guid.NewGuid();
        user.AssignRole(roleId);

        user.RemoveRole(roleId);

        user.RoleAssignments.Should().BeEmpty();
    }
}
