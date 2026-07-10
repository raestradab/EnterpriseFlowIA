namespace EnterpriseFlow.Application.Common;

/// <summary>
/// HU-051: validates a file's real binary signature ("magic bytes") against its claimed
/// extension — never trusting the file name or the <c>Content-Type</c> the client declared,
/// both of which the uploader fully controls and neither of which is a reliable source of
/// truth (same "don't trust client input" principle the security review applied elsewhere,
/// see docs/08a-seguridad.md).
/// </summary>
public static class FileSignatureValidator
{
    // null entries: extensions with no reliable magic-byte signature of their own (plain text
    // has none — any byte sequence is technically valid UTF-8/ASCII) — the extension allowlist
    // itself (DocumentsOptions.AllowedExtensions) is the only gate for those.
    private static readonly Dictionary<string, byte[][]?> SignaturesByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = [[0x25, 0x50, 0x44, 0x46]], // %PDF
        [".png"] = [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]],
        [".jpg"] = [[0xFF, 0xD8, 0xFF]],
        [".jpeg"] = [[0xFF, 0xD8, 0xFF]],
        [".docx"] = [[0x50, 0x4B, 0x03, 0x04]], // OOXML formats are ZIP archives under the hood.
        [".xlsx"] = [[0x50, 0x4B, 0x03, 0x04]],
        [".txt"] = null,
    };

    /// <summary>True if <paramref name="header"/> (the file's leading bytes) matches a known
    /// signature for <paramref name="extension"/>, or if the extension has no reliable
    /// signature to check. False for an extension this validator doesn't recognize at all —
    /// callers should reject unknown extensions via <c>DocumentsOptions.AllowedExtensions</c>
    /// before ever reaching this check.</summary>
    public static bool MatchesExtension(ReadOnlySpan<byte> header, string extension)
    {
        if (!SignaturesByExtension.TryGetValue(extension, out var signatures))
        {
            return false;
        }

        if (signatures is null)
        {
            return true;
        }

        foreach (var signature in signatures)
        {
            if (header.Length >= signature.Length && header[..signature.Length].SequenceEqual(signature))
            {
                return true;
            }
        }

        return false;
    }
}
