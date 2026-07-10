using EnterpriseFlow.Application.Common;
using FluentAssertions;

namespace EnterpriseFlow.Application.UnitTests.Common;

public class FileSignatureValidatorTests
{
    [Fact]
    public void MatchesExtension_Pdf_With_Real_Pdf_Header_Returns_True()
    {
        byte[] header = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x37];

        FileSignatureValidator.MatchesExtension(header, ".pdf").Should().BeTrue();
    }

    [Fact]
    public void MatchesExtension_Pdf_With_An_Exe_Header_Returns_False()
    {
        // HU-051's actual security scenario: a file renamed "installer.exe" to "invoice.pdf"
        // must not pass just because the extension says .pdf.
        byte[] exeHeader = [0x4D, 0x5A, 0x90, 0x00]; // "MZ" — Windows PE header.

        FileSignatureValidator.MatchesExtension(exeHeader, ".pdf").Should().BeFalse();
    }

    [Fact]
    public void MatchesExtension_Png_With_Real_Png_Header_Returns_True()
    {
        byte[] header = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        FileSignatureValidator.MatchesExtension(header, ".png").Should().BeTrue();
    }

    [Fact]
    public void MatchesExtension_Jpeg_And_Jpg_Share_The_Same_Signature()
    {
        byte[] header = [0xFF, 0xD8, 0xFF, 0xE0];

        FileSignatureValidator.MatchesExtension(header, ".jpg").Should().BeTrue();
        FileSignatureValidator.MatchesExtension(header, ".jpeg").Should().BeTrue();
    }

    [Fact]
    public void MatchesExtension_Docx_And_Xlsx_Accept_The_Shared_Zip_Signature()
    {
        byte[] header = [0x50, 0x4B, 0x03, 0x04];

        FileSignatureValidator.MatchesExtension(header, ".docx").Should().BeTrue();
        FileSignatureValidator.MatchesExtension(header, ".xlsx").Should().BeTrue();
    }

    [Fact]
    public void MatchesExtension_Txt_Has_No_Signature_To_Check_Accepts_Anything()
    {
        byte[] header = [0x00, 0x01, 0x02];

        FileSignatureValidator.MatchesExtension(header, ".txt").Should().BeTrue();
    }

    [Fact]
    public void MatchesExtension_Unknown_Extension_Returns_False()
    {
        byte[] header = [0x25, 0x50, 0x44, 0x46];

        FileSignatureValidator.MatchesExtension(header, ".exe").Should().BeFalse();
    }

    [Fact]
    public void MatchesExtension_Header_Shorter_Than_Signature_Returns_False()
    {
        byte[] tooShort = [0x25, 0x50];

        FileSignatureValidator.MatchesExtension(tooShort, ".pdf").Should().BeFalse();
    }
}
