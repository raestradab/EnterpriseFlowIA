using EnterpriseFlow.Domain.Entities;
using FluentAssertions;

namespace EnterpriseFlow.Domain.UnitTests.Entities;

public class RefreshTokenTests
{
    [Fact]
    public void Create_Without_UserId_Throws()
    {
        var act = () => RefreshToken.Create(Guid.Empty, "hash", DateTimeOffset.UtcNow.AddDays(7));

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_With_Missing_TokenHash_Throws(string tokenHash)
    {
        var act = () => RefreshToken.Create(Guid.NewGuid(), tokenHash, DateTimeOffset.UtcNow.AddDays(7));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AssignTenant_Sets_The_TenantId()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", DateTimeOffset.UtcNow.AddDays(7));
        var tenantId = Guid.NewGuid();

        token.AssignTenant(tenantId);

        token.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void IsActive_When_Fresh_Is_True()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", DateTimeOffset.UtcNow.AddDays(7));

        token.IsActive(DateTimeOffset.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void IsActive_When_Expired_Is_False()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", DateTimeOffset.UtcNow.AddSeconds(-1));

        token.IsActive(DateTimeOffset.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void IsActive_When_Used_Is_False()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", DateTimeOffset.UtcNow.AddDays(7));

        token.MarkUsed(Guid.NewGuid());

        token.IsActive(DateTimeOffset.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void IsActive_When_Revoked_Is_False()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", DateTimeOffset.UtcNow.AddDays(7));

        token.Revoke();

        token.IsActive(DateTimeOffset.UtcNow).Should().BeFalse();
    }
}
