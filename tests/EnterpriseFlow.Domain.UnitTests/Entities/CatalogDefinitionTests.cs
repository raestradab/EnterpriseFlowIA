using EnterpriseFlow.Domain.Entities;
using EnterpriseFlow.Domain.Exceptions;
using FluentAssertions;

namespace EnterpriseFlow.Domain.UnitTests.Entities;

public class CatalogDefinitionTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_With_Missing_Name_Throws(string name)
    {
        var act = () => CatalogDefinition.Create(name);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Trims_The_Name()
    {
        var catalog = CatalogDefinition.Create("  Document Categories  ");

        catalog.Name.Should().Be("Document Categories");
    }

    [Fact]
    public void AssignTenant_Sets_The_TenantId()
    {
        var catalog = CatalogDefinition.Create("Document Categories");
        var tenantId = Guid.NewGuid();

        catalog.AssignTenant(tenantId);

        catalog.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void MarkDeleted_Sets_IsDeleted_And_DeletedAtUtc()
    {
        var catalog = CatalogDefinition.Create("Document Categories");

        catalog.MarkDeleted();

        catalog.IsDeleted.Should().BeTrue();
        catalog.DeletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void AddItem_Adds_A_New_Entry()
    {
        var catalog = CatalogDefinition.Create("Document Categories");

        catalog.AddItem("contract", "Contract");

        catalog.Items.Should().ContainSingle(i => i.Key == "contract" && i.Label == "Contract");
    }

    [Fact]
    public void AddItem_Trims_Key_And_Label()
    {
        var catalog = CatalogDefinition.Create("Document Categories");

        catalog.AddItem("  contract  ", "  Contract  ");

        catalog.Items.Should().ContainSingle(i => i.Key == "contract" && i.Label == "Contract");
    }

    [Fact]
    public void AddItem_Duplicate_Key_Throws()
    {
        var catalog = CatalogDefinition.Create("Document Categories");
        catalog.AddItem("contract", "Contract");

        var act = () => catalog.AddItem("contract", "Contract (again)");

        act.Should().Throw<DuplicateCatalogItemKeyException>();
        catalog.Items.Should().HaveCount(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddItem_With_Missing_Key_Throws(string key)
    {
        var catalog = CatalogDefinition.Create("Document Categories");

        var act = () => catalog.AddItem(key, "Contract");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddItem_With_Missing_Label_Throws(string label)
    {
        var catalog = CatalogDefinition.Create("Document Categories");

        var act = () => catalog.AddItem("contract", label);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateItemLabel_Changes_The_Label()
    {
        var catalog = CatalogDefinition.Create("Document Categories");
        catalog.AddItem("contract", "Contract");
        var itemId = catalog.Items.Single().Id;

        catalog.UpdateItemLabel(itemId, "Signed Contract");

        catalog.Items.Single().Label.Should().Be("Signed Contract");
    }

    [Fact]
    public void UpdateItemLabel_Unknown_Item_Throws()
    {
        var catalog = CatalogDefinition.Create("Document Categories");

        var act = () => catalog.UpdateItemLabel(Guid.NewGuid(), "Signed Contract");

        act.Should().Throw<CatalogItemNotFoundException>();
    }

    [Fact]
    public void RemoveItem_Removes_The_Entry()
    {
        var catalog = CatalogDefinition.Create("Document Categories");
        catalog.AddItem("contract", "Contract");
        var itemId = catalog.Items.Single().Id;

        catalog.RemoveItem(itemId);

        catalog.Items.Should().BeEmpty();
    }
}
