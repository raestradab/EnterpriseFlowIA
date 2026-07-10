using EnterpriseFlow.Domain.Entities;
using EnterpriseFlow.Domain.Enums;
using EnterpriseFlow.Domain.Events;
using EnterpriseFlow.Domain.Exceptions;
using FluentAssertions;

namespace EnterpriseFlow.Domain.UnitTests.Entities;

public class DocumentTests
{
    private static Document CreateDocument() =>
        Document.Create("contract.pdf", "application/pdf", 1024, DocumentOwnerType.Project, Guid.NewGuid(), "storage-key-1", Guid.NewGuid());

    [Fact]
    public void Create_With_Valid_Data_Succeeds()
    {
        var initialStateId = Guid.NewGuid();
        var document = Document.Create("contract.pdf", "application/pdf", 1024, DocumentOwnerType.Project, Guid.NewGuid(), "storage-key-1", initialStateId);

        document.FileName.Should().Be("contract.pdf");
        document.CurrentWorkflowStateId.Should().Be(initialStateId);
    }

    [Fact]
    public void Create_Raises_DocumentUploadedDomainEvent()
    {
        var document = CreateDocument();

        document.DomainEvents.Should().ContainSingle(e => e is DocumentUploadedDomainEvent)
            .Which.Should().BeOfType<DocumentUploadedDomainEvent>()
            .Which.DocumentId.Should().Be(document.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_With_Missing_FileName_Throws(string fileName)
    {
        var act = () => Document.Create(fileName, "application/pdf", 1024, DocumentOwnerType.Project, Guid.NewGuid(), "key", Guid.NewGuid());

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_With_Missing_ContentType_Throws(string contentType)
    {
        var act = () => Document.Create("contract.pdf", contentType, 1024, DocumentOwnerType.Project, Guid.NewGuid(), "key", Guid.NewGuid());

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_With_Invalid_Size_Throws(long sizeBytes)
    {
        var act = () => Document.Create("contract.pdf", "application/pdf", sizeBytes, DocumentOwnerType.Project, Guid.NewGuid(), "key", Guid.NewGuid());

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_Without_OwnerId_Throws()
    {
        var act = () => Document.Create("contract.pdf", "application/pdf", 1024, DocumentOwnerType.Project, Guid.Empty, "key", Guid.NewGuid());

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_With_Missing_StorageKey_Throws(string storageKey)
    {
        var act = () => Document.Create("contract.pdf", "application/pdf", 1024, DocumentOwnerType.Project, Guid.NewGuid(), storageKey, Guid.NewGuid());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Without_InitialWorkflowState_Throws()
    {
        var act = () => Document.Create("contract.pdf", "application/pdf", 1024, DocumentOwnerType.Project, Guid.NewGuid(), "key", Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AssignTenant_Sets_The_TenantId()
    {
        var document = CreateDocument();
        var tenantId = Guid.NewGuid();

        document.AssignTenant(tenantId);

        document.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void MarkDeleted_Sets_IsDeleted_And_DeletedAtUtc()
    {
        var document = CreateDocument();

        document.MarkDeleted();

        document.IsDeleted.Should().BeTrue();
        document.DeletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void TransitionTo_When_Allowed_Updates_State_And_Raises_Event()
    {
        var document = CreateDocument();
        var targetStateId = Guid.NewGuid();

        document.TransitionTo(targetStateId, "En Revisión", isTransitionAllowed: true);

        document.CurrentWorkflowStateId.Should().Be(targetStateId);
        document.DomainEvents.Should().ContainSingle(e => e is DocumentWorkflowTransitionedDomainEvent)
            .Which.Should().BeOfType<DocumentWorkflowTransitionedDomainEvent>()
            .Which.ToStateName.Should().Be("En Revisión");
    }

    [Fact]
    public void TransitionTo_When_Not_Allowed_Throws_And_Does_Not_Change_State()
    {
        var document = CreateDocument();
        var originalStateId = document.CurrentWorkflowStateId;

        var act = () => document.TransitionTo(Guid.NewGuid(), "Aprobado", isTransitionAllowed: false);

        act.Should().Throw<InvalidWorkflowTransitionException>();
        document.CurrentWorkflowStateId.Should().Be(originalStateId);
        document.DomainEvents.Should().NotContain(e => e is DocumentWorkflowTransitionedDomainEvent);
    }
}
