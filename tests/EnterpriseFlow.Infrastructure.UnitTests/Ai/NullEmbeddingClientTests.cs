using EnterpriseFlow.Infrastructure.Ai;
using FluentAssertions;

namespace EnterpriseFlow.Infrastructure.UnitTests.Ai;

public class NullEmbeddingClientTests
{
    [Fact]
    public async Task GenerateEmbeddingsAsync_Returns_An_Empty_List()
    {
        var sut = new NullEmbeddingClient();

        var result = await sut.GenerateEmbeddingsAsync(["texto uno", "texto dos"], CancellationToken.None);

        result.Should().BeEmpty();
    }
}
