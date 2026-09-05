using Azure;
using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using TryMudBlazor.Server.Utilities;
using static TryMudBlazor.Server.Utilities.SnippetsEncoder;

namespace TryMudBlazor.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SnippetsController : ControllerBase
{
    private readonly BlobContainerClient _containerClient;

    public SnippetsController(IConfiguration config)
    {
        var snippetsContainerUrl = config["SnippetsContainerUrl"];
        var accessKey = config["SnippetsAccessKey"];

        if (string.IsNullOrEmpty(snippetsContainerUrl) || string.IsNullOrEmpty(accessKey))
        {
            throw new Exception("Please configure SnippetsContainerUrl and SnippetsAccessKey in appsettings.json");
        }

        var containerUri = new Uri(snippetsContainerUrl);

        if (accessKey == "secret")
        {
            var defaultAzureCredentialOptions = new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = config["ManagedCredentialsId"]
            };
            _containerClient = new BlobContainerClient(containerUri,
                new DefaultAzureCredential(defaultAzureCredentialOptions));
        }
        else
        {
            var blobUri = new BlobUriBuilder(containerUri);
            var accountName = blobUri.AccountName;
            var key = new StorageSharedKeyCredential(accountName, accessKey);
            _containerClient = new BlobContainerClient(containerUri, key);
        }
    }

    [HttpGet("{snippetId}")]
    public async Task<IActionResult> Get(string snippetId)
    {
        string decodedSnippetId;
        try
        {
            decodedSnippetId = DecodeSnippetId(snippetId);
        }
        catch (InvalidDataException)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "Invalid snippet ID.");
        }

        var blob = _containerClient.GetBlobClient(SnippetIds.BlobPath(decodedSnippetId));
        var zipStream = new MemoryStream();
        try
        {
            var response = await blob.DownloadAsync();
            await response.Value.Content.CopyToAsync(zipStream);
        }
        catch (RequestFailedException exception) when (exception.Status == StatusCodes.Status404NotFound)
        {
            return NotFound();
        }

        zipStream.Position = 0;

        return File(zipStream, "application/octet-stream", "snippet.zip");
    }

    [HttpPost]
    [RequestSizeLimit(SnippetArchiveValidator.MaxArchiveBytes)]
    public async Task<IActionResult> Post()
    {
        // Buffer the upload so it can be validated before anything reaches storage.
        var archiveStream = new MemoryStream();
        await Request.Body.CopyToAsync(archiveStream);
        archiveStream.Position = 0;

        var validationError = SnippetArchiveValidator.Validate(archiveStream);
        if (validationError is not null)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: validationError);
        }

        var newSnippetId = await UploadWithFreshIdAsync(archiveStream);

        return Ok(EncodeSnippetId(newSnippetId));
    }

    // IDs are the millisecond of the day, so two saves in the same millisecond collide and the second
    // upload fails with 409 BlobAlreadyExists. Retry with a fresh ID rather than failing the save.
    private async Task<string> UploadWithFreshIdAsync(MemoryStream archiveStream)
    {
        const int attempts = 5;
        for (var attempt = 1; ; attempt++)
        {
            var snippetId = SnippetIds.New();
            archiveStream.Position = 0;
            try
            {
                await _containerClient.UploadBlobAsync(SnippetIds.BlobPath(snippetId), archiveStream);
                return snippetId;
            }
            catch (RequestFailedException exception) when (exception.Status == StatusCodes.Status409Conflict && attempt < attempts)
            {
                await Task.Delay(1);
            }
        }
    }
}
