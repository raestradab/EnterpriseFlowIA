using EnterpriseFlow.Domain.Entities;
using EnterpriseFlow.Domain.Events;
using FluentAssertions;

namespace EnterpriseFlow.Domain.UnitTests.Entities;

public class ClientTests
{
    [Fact]
    public void Create_With_Valid_Name_Succeeds()
    {
        var client = Client.Create("Contoso", null);

        client.Name.Should().Be("Contoso");
        client.CompanyId.Should().BeNull();
    }

    [Fact]
    public void Create_With_CompanyId_Associates_It()
    {
        var companyId = Guid.NewGuid();

        var client = Client.Create("Contoso", companyId);

        client.CompanyId.Should().Be(companyId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_With_Missing_Name_Throws(string name)
    {
        var act = () => Client.Create(name, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Deactivate_Marks_Deleted_And_Raises_Cascade_Event()
    {
        var client = Client.Create("Contoso", null);

        client.Deactivate();

        client.IsDeleted.Should().BeTrue();
        client.DomainEvents.Should().ContainSingle(e => e is ClientDeactivatedDomainEvent);
    }
}
