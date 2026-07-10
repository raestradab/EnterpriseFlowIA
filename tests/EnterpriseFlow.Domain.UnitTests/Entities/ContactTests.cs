using EnterpriseFlow.Domain.Entities;
using FluentAssertions;

namespace EnterpriseFlow.Domain.UnitTests.Entities;

public class ContactTests
{
    [Fact]
    public void Create_With_Valid_Data_Succeeds()
    {
        var clientId = Guid.NewGuid();

        var contact = Contact.Create("Jane Doe", "jane@acme.com", "555-1234", clientId);

        contact.Name.Should().Be("Jane Doe");
        contact.Email.Should().Be("jane@acme.com");
        contact.ClientId.Should().Be(clientId);
    }

    [Fact]
    public void Create_Without_ClientId_Throws()
    {
        var act = () => Contact.Create("Jane Doe", null, null, Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_With_Missing_Name_Throws(string name)
    {
        var act = () => Contact.Create(name, null, null, Guid.NewGuid());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Normalizes_Empty_Optional_Fields_To_Null()
    {
        var contact = Contact.Create("Jane Doe", "   ", "   ", Guid.NewGuid());

        contact.Email.Should().BeNull();
        contact.Phone.Should().BeNull();
    }
}
