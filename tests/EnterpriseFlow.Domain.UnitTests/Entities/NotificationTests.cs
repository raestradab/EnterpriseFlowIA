using EnterpriseFlow.Domain.Entities;
using FluentAssertions;

namespace EnterpriseFlow.Domain.UnitTests.Entities;

public class NotificationTests
{
    [Fact]
    public void Create_With_Valid_Data_Succeeds()
    {
        var userId = Guid.NewGuid();

        var notification = Notification.Create(userId, "document.approved", "Your document was approved.");

        notification.UserId.Should().Be(userId);
        notification.IsRead.Should().BeFalse();
    }

    [Fact]
    public void Create_Trims_EventName_And_Message()
    {
        var notification = Notification.Create(Guid.NewGuid(), "  document.approved  ", "  Your document was approved.  ");

        notification.EventName.Should().Be("document.approved");
        notification.Message.Should().Be("Your document was approved.");
    }

    [Fact]
    public void Create_Without_UserId_Throws()
    {
        var act = () => Notification.Create(Guid.Empty, "document.approved", "message");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_With_Missing_EventName_Throws(string eventName)
    {
        var act = () => Notification.Create(Guid.NewGuid(), eventName, "message");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_With_Missing_Message_Throws(string message)
    {
        var act = () => Notification.Create(Guid.NewGuid(), "document.approved", message);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AssignTenant_Sets_The_TenantId()
    {
        var notification = Notification.Create(Guid.NewGuid(), "document.approved", "message");
        var tenantId = Guid.NewGuid();

        notification.AssignTenant(tenantId);

        notification.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void MarkRead_Sets_IsRead()
    {
        var notification = Notification.Create(Guid.NewGuid(), "document.approved", "message");

        notification.MarkRead();

        notification.IsRead.Should().BeTrue();
    }
}
