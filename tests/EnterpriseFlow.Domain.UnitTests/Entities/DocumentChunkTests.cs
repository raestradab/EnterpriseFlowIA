using EnterpriseFlow.Domain.Entities;
using FluentAssertions;

namespace EnterpriseFlow.Domain.UnitTests.Entities;

public class DocumentChunkTests
{
    private static byte[] SampleEmbedding => [0x00, 0x01, 0x02, 0x03];

    [Fact]
    public void Create_With_Valid_Data_Succeeds()
    {
        var documentId = Guid.NewGuid();

        var chunk = DocumentChunk.Create(documentId, 0, "Texto del contrato, parte 1.", SampleEmbedding);

        chunk.DocumentId.Should().Be(documentId);
        chunk.ChunkIndex.Should().Be(0);
        chunk.Content.Should().Be("Texto del contrato, parte 1.");
        chunk.Embedding.Should().Equal(SampleEmbedding);
    }

    [Fact]
    public void Create_Trims_Content()
    {
        var chunk = DocumentChunk.Create(Guid.NewGuid(), 0, "  con espacios  ", SampleEmbedding);

        chunk.Content.Should().Be("con espacios");
    }

    [Fact]
    public void Create_Without_DocumentId_Throws()
    {
        var act = () => DocumentChunk.Create(Guid.Empty, 0, "Texto", SampleEmbedding);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_With_Negative_ChunkIndex_Throws()
    {
        var act = () => DocumentChunk.Create(Guid.NewGuid(), -1, "Texto", SampleEmbedding);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_With_Missing_Content_Throws(string content)
    {
        var act = () => DocumentChunk.Create(Guid.NewGuid(), 0, content, SampleEmbedding);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_With_Empty_Embedding_Throws()
    {
        var act = () => DocumentChunk.Create(Guid.NewGuid(), 0, "Texto", []);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AssignTenant_Sets_The_TenantId()
    {
        var chunk = DocumentChunk.Create(Guid.NewGuid(), 0, "Texto", SampleEmbedding);
        var tenantId = Guid.NewGuid();

        chunk.AssignTenant(tenantId);

        chunk.TenantId.Should().Be(tenantId);
    }
}
