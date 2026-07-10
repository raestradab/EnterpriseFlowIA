namespace EnterpriseFlow.Application.Common;

/// <summary>ADR-0014: the actual ranking math for RAG retrieval — deliberately not on
/// <c>DocumentChunk</c> itself (see that entity's doc comment); a pure, stateless calculation
/// used by <c>SearchDocumentChunksQueryHandler</c>.</summary>
public static class CosineSimilarity
{
    public static double Compute(float[] a, float[] b)
    {
        if (a.Length != b.Length)
        {
            throw new ArgumentException("Vectors must have the same length.", nameof(b));
        }

        double dot = 0, magnitudeA = 0, magnitudeB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magnitudeA += a[i] * a[i];
            magnitudeB += b[i] * b[i];
        }

        if (magnitudeA == 0 || magnitudeB == 0)
        {
            return 0;
        }

        return dot / (Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));
    }
}
