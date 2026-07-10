using System.Text;
using BenchmarkDotNet.Attributes;
using EnterpriseFlow.Application.Common;

namespace EnterpriseFlow.Benchmarks;

/// <summary>F12.4 (Release 4). Runs once per Document uploaded (Sprint 7b's
/// IndexDocumentOnUploadHandler, synchronously inside the upload request) — its cost adds
/// directly to how long a user waits for their upload to finish, unlike RAG's search-time
/// similarity calculation, which happens off the critical path of any write.</summary>
[MemoryDiagnoser]
public class TextChunkerBenchmarks
{
    // Roughly a 10-page extracted PDF/Word document — a realistic real-world upload, not an
    // arbitrary size picked to make the benchmark look good either way.
    private const int DocumentSizeChars = 25_000;

    private string _document = null!;

    [GlobalSetup]
    public void Setup()
    {
        var builder = new StringBuilder(DocumentSizeChars);
        var random = new Random(42);
        while (builder.Length < DocumentSizeChars)
        {
            builder.Append((char)('a' + random.Next(26)));
            if (random.Next(8) == 0)
            {
                builder.Append(' ');
            }
        }

        _document = builder.ToString();
    }

    [Benchmark]
    public IReadOnlyList<string> SplitDocument() => TextChunker.Split(_document);
}
