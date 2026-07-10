namespace EnterpriseFlow.Infrastructure.Storage;

/// <summary>
/// Bound from the <c>Documents</c> configuration section. <see cref="Provider"/> selects which
/// <c>IDocumentStorageProvider</c> implementation <c>DependencyInjection</c> registers — the
/// only thing that needs to change to swap providers (ADR-0009, F5.6). File validation limits
/// (max size, allowed extensions) live in <c>Application.Common.DocumentValidationOptions</c>
/// instead, bound from the same config section — Application can't reference this Infrastructure
/// type (ADR-0002), but it needs those two settings for HU-051.
/// </summary>
public sealed class DocumentsOptions
{
    public const string SectionName = "Documents";

    /// <summary>Gets one of "Local", "AzureBlob", "S3", "Gcs" — case-insensitive.</summary>
    public string Provider { get; init; } = "Local";
}

public sealed class LocalStorageOptions
{
    public const string SectionName = "Documents:Local";

    public string BasePath { get; init; } = "App_Data/documents";
}

public sealed class AzureBlobStorageOptions
{
    public const string SectionName = "Documents:AzureBlob";

    public required string ConnectionString { get; init; }

    public string ContainerName { get; init; } = "documents";
}

public sealed class S3StorageOptions
{
    public const string SectionName = "Documents:S3";

    public required string BucketName { get; init; }

    public string Region { get; init; } = "us-east-1";

    /// <summary>Gets the endpoint override used only for S3-compatible local testing (e.g.
    /// LocalStack/MinIO) — null uses the real AWS endpoint for the configured region.</summary>
    public string? ServiceUrl { get; init; }
}

public sealed class GcsStorageOptions
{
    public const string SectionName = "Documents:Gcs";

    public required string BucketName { get; init; }
}
