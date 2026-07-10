using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using EnterpriseFlow.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace EnterpriseFlow.Infrastructure.Storage;

/// <summary>F5.3 — real Azure SDK, not a stub (r2-01-vision-y-alcance.md, sección 3). Not
/// runtime-verified in this environment (no Azure account / Azurite instance available) — see
/// r2-07b-backend-documentos.md for what was and wasn't verified.</summary>
public sealed class AzureBlobStorageProvider : IDocumentStorageProvider
{
    private readonly BlobContainerClient _container;

    public AzureBlobStorageProvider(IOptions<AzureBlobStorageOptions> options)
    {
        _container = new BlobContainerClient(options.Value.ConnectionString, options.Value.ContainerName);
        _container.CreateIfNotExists();
    }

    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken)
    {
        var storageKey = $"{Guid.NewGuid():N}{Path.GetExtension(fileName)}";

        await _container.GetBlobClient(storageKey).UploadAsync(
            content,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
            cancellationToken);

        return storageKey;
    }

    public async Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken)
    {
        var response = await _container.GetBlobClient(storageKey).DownloadStreamingAsync(cancellationToken: cancellationToken);
        return response.Value.Content;
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken) =>
        await _container.GetBlobClient(storageKey).DeleteIfExistsAsync(cancellationToken: cancellationToken);
}
