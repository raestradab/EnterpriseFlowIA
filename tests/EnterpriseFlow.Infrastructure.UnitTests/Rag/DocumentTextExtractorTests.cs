using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using EnterpriseFlow.Infrastructure.Rag;
using FluentAssertions;

namespace EnterpriseFlow.Infrastructure.UnitTests.Rag;

public class DocumentTextExtractorTests
{
    private readonly DocumentTextExtractor _sut = new();

    [Fact]
    public void ExtractText_From_Txt_Returns_The_Raw_Content()
    {
        var result = _sut.ExtractText("contrato.txt", Encoding.UTF8.GetBytes("Cláusula 1: el plazo es de 12 meses."));

        result.Should().Be("Cláusula 1: el plazo es de 12 meses.");
    }

    [Fact]
    public void ExtractText_From_Docx_Returns_The_Paragraph_Text()
    {
        var content = BuildMinimalDocx("Cláusula 1: el plazo es de 12 meses.");

        var result = _sut.ExtractText("contrato.docx", content);

        result.Should().Contain("Cláusula 1: el plazo es de 12 meses.");
    }

    [Fact]
    public void ExtractText_From_Pdf_Returns_The_Page_Text()
    {
        var content = BuildMinimalPdf("Hello RAG");

        var result = _sut.ExtractText("contrato.pdf", content);

        result.Should().Contain("Hello RAG");
    }

    [Theory]
    [InlineData("logo.png")]
    [InlineData("hoja.xlsx")]
    [InlineData("foto.jpg")]
    public void ExtractText_From_An_Unsupported_Extension_Returns_Null(string fileName)
    {
        var result = _sut.ExtractText(fileName, [0x01, 0x02, 0x03]);

        result.Should().BeNull();
    }

    [Fact]
    public void ExtractText_From_Blank_Content_Returns_Null_Not_An_Empty_String()
    {
        var result = _sut.ExtractText("vacio.txt", Encoding.UTF8.GetBytes("   \n\t  "));

        result.Should().BeNull();
    }

    private static byte[] BuildMinimalDocx(string paragraphText)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(new Body(new Paragraph(new Run(new Text(paragraphText)))));
            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    private static byte[] BuildMinimalPdf(string text)
    {
        var objects = new List<string>
        {
            "<</Type/Catalog/Pages 2 0 R>>",
            "<</Type/Pages/Kids[3 0 R]/Count 1>>",
            "<</Type/Page/Parent 2 0 R/Resources<</Font<</F1 4 0 R>>>>/MediaBox[0 0 200 200]/Contents 5 0 R>>",
            "<</Type/Font/Subtype/Type1/BaseFont/Helvetica>>",
        };

        var contentStream = $"BT /F1 12 Tf 10 100 Td ({text}) Tj ET";
        var streamObject = $"<</Length {contentStream.Length}>>\nstream\n{contentStream}\nendstream";

        var sb = new StringBuilder();
        sb.Append("%PDF-1.4\n");

        var offsets = new List<int>();
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(sb.Length);
            sb.Append($"{i + 1} 0 obj{objects[i]}endobj\n");
        }

        offsets.Add(sb.Length);
        sb.Append($"5 0 obj{streamObject}endobj\n");

        var xrefOffset = sb.Length;
        sb.Append("xref\n0 6\n0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            sb.Append($"{offset:D10} 00000 n \n");
        }

        sb.Append($"trailer<</Size 6/Root 1 0 R>>\nstartxref\n{xrefOffset}\n%%EOF");

        return Encoding.ASCII.GetBytes(sb.ToString());
    }
}
