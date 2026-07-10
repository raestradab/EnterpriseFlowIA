using BenchmarkDotNet.Attributes;
using EnterpriseFlow.Application.Common;

namespace EnterpriseFlow.Benchmarks;

/// <summary>F12.4 (Release 4). ADR-0014's RAG retrieval computes this once per chunk in a
/// tenant's corpus for every question asked to the assistant — the one closed-form calculation
/// in the whole search path, worth measuring for real instead of assuming SQL Server-scale
/// vector search would obviously be needed sooner.</summary>
[MemoryDiagnoser]
public class CosineSimilarityBenchmarks
{
    // text-embedding-3-small (OpenAiEmbeddingClient, Release 3) produces 1536-dimension vectors —
    // the real shape this calculation runs against, not an arbitrary round number.
    private const int Dimensions = 1536;

    private float[] _query = null!;
    private float[] _candidate = null!;

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);
        _query = CreateRandomVector(random);
        _candidate = CreateRandomVector(random);
    }

    [Benchmark]
    public double ComputeSimilarity() => CosineSimilarity.Compute(_query, _candidate);

    private static float[] CreateRandomVector(Random random)
    {
        var vector = new float[Dimensions];
        for (var i = 0; i < Dimensions; i++)
        {
            vector[i] = (float)((random.NextDouble() * 2) - 1);
        }

        return vector;
    }
}
