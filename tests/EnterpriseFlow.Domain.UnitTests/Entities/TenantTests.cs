using EnterpriseFlow.Domain.Entities;
using FluentAssertions;

namespace EnterpriseFlow.Domain.UnitTests.Entities;

public class TenantTests
{
    [Fact]
    public void Create_With_Valid_Data_Normalizes_Slug()
    {
        var tenant = Tenant.Create("  Acme Corp  ", "  ACME-Corp  ");

        tenant.Name.Should().Be("Acme Corp");
        tenant.Slug.Should().Be("acme-corp");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_With_Missing_Name_Throws(string name)
    {
        var act = () => Tenant.Create(name, "acme-corp");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_With_Missing_Slug_Throws(string slug)
    {
        var act = () => Tenant.Create("Acme Corp", slug);

        act.Should().Throw<ArgumentException>();
    }
}
