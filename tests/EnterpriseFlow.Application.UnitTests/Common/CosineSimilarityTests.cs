using EnterpriseFlow.Application.Common;
using FluentAssertions;

namespace EnterpriseFlow.Application.UnitTests.Common;

public class CosineSimilarityTests
{
    [Fact]
    public void Identical_Vectors_Have_Similarity_One()
    {
        float[] a = [1f, 2f, 3f];

        CosineSimilarity.Compute(a, a).Should().BeApproximately(1.0, 0.0001);
    }

    [Fact]
    public void Opposite_Vectors_Have_Similarity_Negative_One()
    {
        float[] a = [1f, 0f];
        float[] b = [-1f, 0f];

        CosineSimilarity.Compute(a, b).Should().BeApproximately(-1.0, 0.0001);
    }

    [Fact]
    public void Orthogonal_Vectors_Have_Similarity_Zero()
    {
        float[] a = [1f, 0f];
        float[] b = [0f, 1f];

        CosineSimilarity.Compute(a, b).Should().BeApproximately(0.0, 0.0001);
    }

    [Fact]
    public void A_Zero_Vector_Has_Similarity_Zero_Not_NaN()
    {
        float[] a = [0f, 0f];
        float[] b = [1f, 1f];

        CosineSimilarity.Compute(a, b).Should().Be(0);
    }

    [Fact]
    public void Vectors_Of_Different_Length_Throw()
    {
        float[] a = [1f, 2f];
        float[] b = [1f, 2f, 3f];

        var act = () => CosineSimilarity.Compute(a, b);

        act.Should().Throw<ArgumentException>();
    }
}
