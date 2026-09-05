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

        var blob = _containerClient.GetBlobClient(BlobPath(decodedSnippetId));
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

        archiveStream.Position = 0;

        var newSnippetId = NewSnippetId();
        await _containerClient.UploadBlobAsync(BlobPath(newSnippetId), archiveStream);

        return Ok(EncodeSnippetId(newSnippetId));
    }

    private static string NewSnippetId()
    {
        var yearFolder = DateTime.Now.Year;
        var monthFolder = DateTime.Now.Month;
        var dayFolder = DateTime.Now.Day;
        var time = Convert.ToInt32(DateTime.Now.TimeOfDay.TotalMilliseconds);
        var snippetTime = $"{time:D8}";

        return $"{yearFolder:0000}{monthFolder:00}{dayFolder:00}{snippetTime:D8}";
    }

    private static string BlobPath(string snippetId)
    {
        var yearFolder = snippetId.Substring(0, 4);
        var monthFolder = snippetId.Substring(4, 2);
        var dayFolder = snippetId.Substring(6, 2);
        var time = snippetId.Substring(8);
        var snippetFolder = $"{yearFolder:0000}/{monthFolder:00}/{dayFolder:00}";
        var snippetTime = $"{time:00000000}";

        return $"{snippetFolder}/{snippetTime}";
    }
}
