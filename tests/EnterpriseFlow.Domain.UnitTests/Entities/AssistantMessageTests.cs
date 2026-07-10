using EnterpriseFlow.Domain.Entities;
using EnterpriseFlow.Domain.Enums;
using FluentAssertions;

namespace EnterpriseFlow.Domain.UnitTests.Entities;

public class AssistantMessageTests
{
    [Fact]
    public void Create_With_Valid_Data_Succeeds()
    {
        var userId = Guid.NewGuid();

        var message = AssistantMessage.Create(userId, AssistantMessageRole.User, "¿Cuántos proyectos activos tengo?");

        message.UserId.Should().Be(userId);
        message.Role.Should().Be(AssistantMessageRole.User);
        message.Content.Should().Be("¿Cuántos proyectos activos tengo?");
    }

    [Fact]
    public void Create_Trims_Content()
    {
        var message = AssistantMessage.Create(Guid.NewGuid(), AssistantMessageRole.Assistant, "  Tenés 3 proyectos activos.  ");

        message.Content.Should().Be("Tenés 3 proyectos activos.");
    }

    [Fact]
    public void Create_Without_UserId_Throws()
    {
        var act = () => AssistantMessage.Create(Guid.Empty, AssistantMessageRole.User, "Hola");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_With_Missing_Content_Throws(string content)
    {
        var act = () => AssistantMessage.Create(Guid.NewGuid(), AssistantMessageRole.User, content);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AssignTenant_Sets_The_TenantId()
    {
        var message = AssistantMessage.Create(Guid.NewGuid(), AssistantMessageRole.User, "Hola");
        var tenantId = Guid.NewGuid();

        message.AssignTenant(tenantId);

        message.TenantId.Should().Be(tenantId);
    }
}
