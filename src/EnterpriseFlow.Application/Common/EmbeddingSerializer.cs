namespace EnterpriseFlow.Application.Common;

/// <summary>ADR-0014: <c>DocumentChunk.Embedding</c> persists as <c>byte[]</c> (a
/// <c>varbinary(max)</c> column) — this is the one place that boundary gets crossed, both when
/// indexing (float[] from <c>IEmbeddingClient</c> → byte[] to persist) and when searching
/// (byte[] read back → float[] to compare). Not endianness-portable across machine architectures
/// — acceptable, since these bytes never leave the single SQL Server database that wrote
/// them.</summary>
public static class EmbeddingSerializer
{
    public static byte[] ToBytes(float[] embedding)
    {
        var bytes = new byte[embedding.Length * sizeof(float)];
        Buffer.BlockCopy(embedding, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    public static float[] ToFloats(byte[] bytes)
    {
        var floats = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
        return floats;
    }
}
