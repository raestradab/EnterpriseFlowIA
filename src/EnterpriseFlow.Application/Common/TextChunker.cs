namespace EnterpriseFlow.Application.Common;

/// <summary>F10.1 (HU-100). Fixed-size character chunking with overlap — good enough for a
/// portfolio-scale corpus; no HU asks for sentence/paragraph-aware splitting, and a naive
/// character window is trivially deterministic to test.</summary>
public static class TextChunker
{
    public const int DefaultChunkSize = 1000;
    public const int DefaultOverlap = 100;

    public static IReadOnlyList<string> Split(string text, int chunkSize = DefaultChunkSize, int overlap = DefaultOverlap)
    {
        if (overlap >= chunkSize)
        {
            throw new ArgumentOutOfRangeException(nameof(overlap), overlap, "Overlap must be smaller than the chunk size.");
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var normalized = text.Trim();
        if (normalized.Length <= chunkSize)
        {
            return [normalized];
        }

        var chunks = new List<string>();
        var start = 0;
        while (start < normalized.Length)
        {
            var length = Math.Min(chunkSize, normalized.Length - start);
            chunks.Add(normalized.Substring(start, length));

            if (start + length >= normalized.Length)
            {
                break;
            }

            start += chunkSize - overlap;
        }

        return chunks;
    }
}
