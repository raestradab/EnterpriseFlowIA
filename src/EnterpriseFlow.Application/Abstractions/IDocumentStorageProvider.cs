namespace EnterpriseFlow.Application.Abstractions;

/// <summary>
/// Storage abstraction for Documents (F5, ADR-0009). Exactly one implementation is active per
/// running instance, selected in <c>Infrastructure.DependencyInjection</c> from configuration —
/// <see cref="Application"/> never knows which one. The <c>storageKey</c> returned by
/// <see cref="UploadAsync"/> is an opaque identifier: never a local file path, never a
/// provider-specific URL. Leaking either through this contract would defeat the point of the
/// abstraction — swapping providers is meant to require configuration only, never a code change.
/// </summary>
public interface IDocumentStorageProvider
{
    Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken);

    Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken);

    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
}
