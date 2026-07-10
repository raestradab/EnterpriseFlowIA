namespace EnterpriseFlow.Application.Common;

/// <summary>
/// HU-051. Bound from the same <c>Documents</c> configuration section as
/// <c>Infrastructure.Storage.DocumentsOptions</c> (which owns <c>Provider</c> instead) —
/// Application can't reference that Infrastructure type (ADR-0002), so the two settings it
/// actually needs get their own lean, framework-agnostic options class here.
/// </summary>
public sealed class DocumentValidationOptions
{
    public const string SectionName = "Documents";

    public long MaxSizeBytes { get; init; } = 25 * 1024 * 1024;

    public string[] AllowedExtensions { get; init; } =
        [".pdf", ".png", ".jpg", ".jpeg", ".docx", ".xlsx", ".txt"];
}
