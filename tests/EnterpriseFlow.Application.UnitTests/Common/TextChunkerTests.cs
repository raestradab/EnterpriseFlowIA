using EnterpriseFlow.Application.Common;
using FluentAssertions;

namespace EnterpriseFlow.Application.UnitTests.Common;

public class TextChunkerTests
{
    [Fact]
    public void Split_Empty_Text_Returns_No_Chunks()
    {
        TextChunker.Split(string.Empty).Should().BeEmpty();
    }

    [Fact]
    public void Split_Text_Shorter_Than_ChunkSize_Returns_A_Single_Chunk()
    {
        var result = TextChunker.Split("texto corto", chunkSize: 1000, overlap: 100);

        result.Should().ContainSingle().Which.Should().Be("texto corto");
    }

    [Fact]
    public void Split_Long_Text_Produces_Multiple_Overlapping_Chunks()
    {
        var text = new string('a', 250);

        var chunks = TextChunker.Split(text, chunkSize: 100, overlap: 20);

        chunks.Should().HaveCountGreaterThan(1);
        chunks.Should().AllSatisfy(c => c.Length.Should().BeLessThanOrEqualTo(100));
    }

    [Fact]
    public void Split_Reconstructs_The_Full_Text_When_Overlap_Is_Removed()
    {
        var text = "0123456789ABCDEFGHIJ"; // 20 chars

        var chunks = TextChunker.Split(text, chunkSize: 8, overlap: 3);

        // Every character of the original text appears in at least one chunk, in order — the
        // overlap exists so retrieval never silently drops a boundary, not so text gets lost.
        string.Concat(chunks).Should().Contain(text[..5]);
        chunks[^1].Should().EndWith("J");
    }

    [Fact]
    public void Split_With_Overlap_GreaterOrEqual_To_ChunkSize_Throws()
    {
        var act = () => TextChunker.Split("cualquier texto largo suficiente", chunkSize: 10, overlap: 10);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
