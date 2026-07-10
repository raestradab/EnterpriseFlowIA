using System.Net;
using EnterpriseFlow.Application.Abstractions;
using Google;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Options;

namespace EnterpriseFlow.Infrastructure.Storage;

/// <summary>F5.5 — real Google Cloud SDK, not a stub (r2-01-vision-y-alcance.md, sección 3).
/// <see cref="StorageClient.Create()"/> resolves Application Default Credentials — throws at
/// construction if none are configured, which only matters if <c>Documents:Provider=Gcs</c> is
/// actually selected (the default provider is Local; see DependencyInjection.cs). Not
/// runtime-verified in this environment (no GCP account / emulator available).</summary>
public sealed class GoogleCloudStorageProvider : IDocumentStorageProvider
{
    private readonly StorageClient _client;
    private readonly string _bucketName;

    public GoogleCloudStorageProvider(IOptions<GcsStorageOptions> options)
    {
        _bucketName = options.Value.BucketName;
        _client = StorageClient.Create();
    }

    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken)
    {
        var storageKey = $"{Guid.NewGuid():N}{Path.GetExtension(fileName)}";

        await _client.UploadObjectAsync(_bucketName, storageKey, contentType, content, cancellationToken: cancellationToken);

        return storageKey;
    }

    public async Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken)
    {
        var buffer = new MemoryStream();
        await _client.DownloadObjectAsync(_bucketName, storageKey, buffer, cancellationToken: cancellationToken);
        buffer.Position = 0;
        return buffer;
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        try
        {
            await _client.DeleteObjectAsync(_bucketName, storageKey, cancellationToken: cancellationToken);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            // Idempotent delete — matches LocalStorageProvider/AzureBlobStorageProvider.
        }
    }
}
