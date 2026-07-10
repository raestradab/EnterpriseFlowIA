using EnterpriseFlow.Domain.Entities;
using FluentAssertions;

namespace EnterpriseFlow.Domain.UnitTests.Entities;

public class CompanyTests
{
    [Fact]
    public void Create_With_Valid_Name_Succeeds()
    {
        var company = Company.Create("Acme Corp", "123-456");

        company.Name.Should().Be("Acme Corp");
        company.TaxId.Should().Be("123-456");
        company.IsDeleted.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_With_Missing_Name_Throws(string? name)
    {
        var act = () => Company.Create(name!, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Trims_Name_And_Normalizes_Empty_TaxId_To_Null()
    {
        var company = Company.Create("  Acme Corp  ", "   ");

        company.Name.Should().Be("Acme Corp");
        company.TaxId.Should().BeNull();
    }

    [Fact]
    public void MarkDeleted_Sets_IsDeleted_And_Timestamp()
    {
        var company = Company.Create("Acme Corp", null);

        company.MarkDeleted();

        company.IsDeleted.Should().BeTrue();
        company.DeletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void AssignTenant_Sets_TenantId()
    {
        var company = Company.Create("Acme Corp", null);
        var tenantId = Guid.NewGuid();

        company.AssignTenant(tenantId);

        company.TenantId.Should().Be(tenantId);
    }
}
