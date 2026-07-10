using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using EnterpriseFlow.Application.Abstractions;
using UglyToad.PdfPig;

namespace EnterpriseFlow.Infrastructure.Rag;

/// <summary>
/// F10.2 (HU-100). Real implementation — always registered (no Null fallback needed: unlike
/// <c>IAiChatClient</c>/<c>IEmbeddingClient</c>, extracting text from a file needs no API key or
/// external service, so there's nothing to gracefully degrade from).
/// </summary>
public sealed class DocumentTextExtractor : IDocumentTextExtractor
{
    public string? ExtractText(string fileName, byte[] content)
    {
        var text = Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".txt" => ExtractPlainText(content),
            ".pdf" => ExtractPdfText(content),
            ".docx" => ExtractDocxText(content),
            _ => null,
        };

        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static string ExtractPlainText(byte[] content) => Encoding.UTF8.GetString(content);

    private static string ExtractPdfText(byte[] content)
    {
        using var document = PdfDocument.Open(content);
        var builder = new StringBuilder();

        foreach (var page in document.GetPages())
        {
            builder.AppendLine(page.Text);
        }

        return builder.ToString();
    }

    private static string ExtractDocxText(byte[] content)
    {
        using var stream = new MemoryStream(content);
        using var wordDocument = WordprocessingDocument.Open(stream, false);

        var body = wordDocument.MainDocumentPart?.Document?.Body;
        if (body is null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            builder.AppendLine(paragraph.InnerText);
        }

        return builder.ToString();
    }
}
