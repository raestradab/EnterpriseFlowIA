using EnterpriseFlow.Application.Common;
using FluentAssertions;

namespace EnterpriseFlow.Application.UnitTests.Common;

public class EmbeddingSerializerTests
{
    [Fact]
    public void ToBytes_Then_ToFloats_RoundTrips_Exactly()
    {
        float[] original = [0.1f, -0.5f, 3.14159f, 0f, 42f];

        var bytes = EmbeddingSerializer.ToBytes(original);
        var restored = EmbeddingSerializer.ToFloats(bytes);

        restored.Should().Equal(original);
    }

    [Fact]
    public void ToBytes_Produces_Four_Bytes_Per_Float()
    {
        float[] embedding = [1f, 2f, 3f];

        var bytes = EmbeddingSerializer.ToBytes(embedding);

        bytes.Should().HaveCount(12);
    }
}
