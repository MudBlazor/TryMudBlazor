using Azure;
using Azure.Storage.Blobs;

namespace TryMudBlazor.Server.Utilities;

public sealed class BlobSnippetStore : ISnippetStore
{
    private readonly BlobContainerClient _containerClient;

    public BlobSnippetStore(BlobContainerClient containerClient)
    {
        _containerClient = containerClient;
    }

    public async Task<Stream?> DownloadAsync(string blobPath, CancellationToken cancellationToken)
    {
        var archive = new MemoryStream();
        try
        {
            var response = await _containerClient.GetBlobClient(blobPath).DownloadAsync(cancellationToken);
            await response.Value.Content.CopyToAsync(archive, cancellationToken);
        }
        catch (RequestFailedException exception) when (exception.Status == StatusCodes.Status404NotFound)
        {
            return null;
        }

        archive.Position = 0;
        return archive;
    }

    public async Task<bool> TryUploadAsync(string blobPath, Stream content, CancellationToken cancellationToken)
    {
        try
        {
            // UploadBlobAsync never overwrites, so an existing blob comes back as a 409 rather than being replaced.
            await _containerClient.UploadBlobAsync(blobPath, content, cancellationToken);
            return true;
        }
        catch (RequestFailedException exception) when (exception.Status == StatusCodes.Status409Conflict)
        {
            return false;
        }
    }
}
