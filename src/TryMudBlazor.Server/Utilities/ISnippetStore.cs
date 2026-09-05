namespace TryMudBlazor.Server.Utilities;

/// <summary>
/// Where snippet archives live, addressed by the blob path SnippetsController derives from the storage ID.
/// </summary>
public interface ISnippetStore
{
    /// <summary>
    /// Returns the archive stored at <paramref name="blobPath"/>, or null when there is none.
    /// </summary>
    Task<Stream?> DownloadAsync(string blobPath, CancellationToken cancellationToken);

    /// <summary>
    /// Stores the archive unless something already exists at <paramref name="blobPath"/>, in which case
    /// nothing is written and false is returned.
    /// </summary>
    Task<bool> TryUploadAsync(string blobPath, Stream content, CancellationToken cancellationToken);
}
