namespace EnterpriseFlow.Application.Abstractions;

/// <summary>F10.2 (HU-100), ADR-0013. Not one interface per format — a single seam by design,
/// since Application never needs to know or care which format a Document is; it dispatches on
/// <paramref name="fileName"/>'s extension internally, same pattern as
/// <see cref="Common.FileSignatureValidator"/> already established for upload validation.</summary>
public interface IDocumentTextExtractor
{
    /// <summary>Returns <see langword="null"/> when the extension isn't supported for RAG, or
    /// when nothing extractable was found (e.g. a scanned PDF with no text layer) — a normal,
    /// expected outcome (HU-100: the Document still gets saved, it just doesn't participate in
    /// RAG), never an exception.</summary>
    string? ExtractText(string fileName, byte[] content);
}
