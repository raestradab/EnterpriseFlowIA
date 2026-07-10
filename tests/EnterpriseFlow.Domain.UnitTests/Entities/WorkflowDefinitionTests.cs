using EnterpriseFlow.Domain.Entities;
using EnterpriseFlow.Domain.Exceptions;
using FluentAssertions;

namespace EnterpriseFlow.Domain.UnitTests.Entities;

public class WorkflowDefinitionTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_With_Missing_Name_Throws(string name)
    {
        var act = () => WorkflowDefinition.Create(name);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Trims_The_Name()
    {
        var workflow = WorkflowDefinition.Create("  Document Approval  ");

        workflow.Name.Should().Be("Document Approval");
    }

    [Fact]
    public void AssignTenant_Sets_The_TenantId()
    {
        var workflow = WorkflowDefinition.Create("Document Approval");
        var tenantId = Guid.NewGuid();

        workflow.AssignTenant(tenantId);

        workflow.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void MarkDeleted_Sets_IsDeleted_And_DeletedAtUtc()
    {
        var workflow = WorkflowDefinition.Create("Document Approval");

        workflow.MarkDeleted();

        workflow.IsDeleted.Should().BeTrue();
        workflow.DeletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void AddState_Adds_A_New_State()
    {
        var workflow = WorkflowDefinition.Create("Document Approval");

        var state = workflow.AddState("Borrador", isInitial: true, isFinal: false);

        workflow.States.Should().ContainSingle(s => s.Id == state.Id && s.Name == "Borrador" && s.IsInitial);
    }

    [Fact]
    public void AddState_Second_Initial_State_Throws()
    {
        var workflow = WorkflowDefinition.Create("Document Approval");
        workflow.AddState("Borrador", isInitial: true, isFinal: false);

        var act = () => workflow.AddState("Otro Inicial", isInitial: true, isFinal: false);

        act.Should().Throw<DuplicateInitialWorkflowStateException>();
        workflow.States.Should().HaveCount(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddState_With_Missing_Name_Throws(string name)
    {
        var workflow = WorkflowDefinition.Create("Document Approval");

        var act = () => workflow.AddState(name, isInitial: false, isFinal: false);

        act.Should().Throw<ArgumentException>();
    }

    private static WorkflowDefinition CreateWorkflowWithTwoStates(out Guid draftId, out Guid reviewId)
    {
        var workflow = WorkflowDefinition.Create("Document Approval");
        draftId = workflow.AddState("Borrador", isInitial: true, isFinal: false).Id;
        reviewId = workflow.AddState("En Revisión", isInitial: false, isFinal: false).Id;
        return workflow;
    }

    [Fact]
    public void AddTransition_Between_Known_States_Succeeds()
    {
        var workflow = CreateWorkflowWithTwoStates(out var draftId, out var reviewId);

        workflow.AddTransition("Enviar a revisión", draftId, reviewId);

        workflow.Transitions.Should().ContainSingle(t => t.FromStateId == draftId && t.ToStateId == reviewId);
        workflow.CanTransition(draftId, reviewId).Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddTransition_With_Missing_Name_Throws(string name)
    {
        var workflow = CreateWorkflowWithTwoStates(out var draftId, out var reviewId);

        var act = () => workflow.AddTransition(name, draftId, reviewId);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddTransition_With_Unknown_FromState_Throws()
    {
        var workflow = CreateWorkflowWithTwoStates(out _, out var reviewId);

        var act = () => workflow.AddTransition("Enviar a revisión", Guid.NewGuid(), reviewId);

        act.Should().Throw<UnknownWorkflowStateException>();
    }

    [Fact]
    public void AddTransition_With_Unknown_ToState_Throws()
    {
        var workflow = CreateWorkflowWithTwoStates(out var draftId, out _);

        var act = () => workflow.AddTransition("Enviar a revisión", draftId, Guid.NewGuid());

        act.Should().Throw<UnknownWorkflowStateException>();
    }

    [Fact]
    public void AddTransition_Duplicate_Throws()
    {
        var workflow = CreateWorkflowWithTwoStates(out var draftId, out var reviewId);
        workflow.AddTransition("Enviar a revisión", draftId, reviewId);

        var act = () => workflow.AddTransition("Otra vez", draftId, reviewId);

        act.Should().Throw<DuplicateWorkflowTransitionException>();
    }

    [Fact]
    public void CanTransition_Without_A_Matching_Transition_Returns_False()
    {
        var workflow = CreateWorkflowWithTwoStates(out var draftId, out var reviewId);

        workflow.CanTransition(reviewId, draftId).Should().BeFalse();
    }
}
