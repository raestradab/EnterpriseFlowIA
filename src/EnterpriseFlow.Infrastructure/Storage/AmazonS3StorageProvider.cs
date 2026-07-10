using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using EnterpriseFlow.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace EnterpriseFlow.Infrastructure.Storage;

/// <summary>F5.4 — real AWS SDK, not a stub (r2-01-vision-y-alcance.md, sección 3). Credentials
/// come from the standard AWS SDK provider chain (environment/profile/IAM role) — never
/// hardcoded, same principle as every other secret in this project (docs/08a-seguridad.md).
/// Not runtime-verified in this environment (no AWS account / LocalStack instance available).</summary>
public sealed class AmazonS3StorageProvider : IDocumentStorageProvider, IDisposable
{
    private readonly AmazonS3Client _client;
    private readonly string _bucketName;

    public AmazonS3StorageProvider(IOptions<S3StorageOptions> options)
    {
        var settings = options.Value;
        _bucketName = settings.BucketName;

        var config = new AmazonS3Config { RegionEndpoint = RegionEndpoint.GetBySystemName(settings.Region) };
        if (!string.IsNullOrWhiteSpace(settings.ServiceUrl))
        {
            // S3-compatible local testing (LocalStack/MinIO) only — real AWS ignores this path.
            config.ServiceURL = settings.ServiceUrl;
            config.ForcePathStyle = true;
        }

        _client = new AmazonS3Client(config);
    }

    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken)
    {
        var storageKey = $"{Guid.NewGuid():N}{Path.GetExtension(fileName)}";

        await _client.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = storageKey,
                InputStream = content,
                ContentType = contentType,
            },
            cancellationToken);

        return storageKey;
    }

    public async Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken)
    {
        var response = await _client.GetObjectAsync(_bucketName, storageKey, cancellationToken);
        return response.ResponseStream;
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken) =>
        await _client.DeleteObjectAsync(_bucketName, storageKey, cancellationToken);

    public void Dispose() => _client.Dispose();
}
