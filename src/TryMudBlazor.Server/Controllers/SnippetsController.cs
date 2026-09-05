using Microsoft.AspNetCore.Mvc;
using TryMudBlazor.Server.Utilities;
using static TryMudBlazor.Server.Utilities.SnippetsEncoder;

namespace TryMudBlazor.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SnippetsController : ControllerBase
{
    /// <summary>
    /// After the timestamp candidate every retry is random within a space of 10^8 per day, so a few attempts
    /// are plenty, and the budget keeps the storage work one anonymous request can cause bounded.
    /// </summary>
    public const int MaxSaveAttempts = 5;

    private readonly ISnippetStore _store;
    private readonly SnippetIdAllocator _idAllocator;

    public SnippetsController(ISnippetStore store, SnippetIdAllocator idAllocator)
    {
        _store = store;
        _idAllocator = idAllocator;
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

        var archive = await _store.DownloadAsync(BlobPath(decodedSnippetId), HttpContext.RequestAborted);
        if (archive is null)
        {
            return NotFound();
        }

        return File(archive, "application/octet-stream", "snippet.zip");
    }

    [HttpPost]
    [RequestSizeLimit(SnippetArchiveValidator.MaxArchiveBytes)]
    public async Task<IActionResult> Post()
    {
        var cancellationToken = HttpContext.RequestAborted;

        // Buffer the upload so it can be validated before anything reaches storage.
        var archiveStream = new MemoryStream();
        await Request.Body.CopyToAsync(archiveStream, cancellationToken);
        archiveStream.Position = 0;

        var validationError = SnippetArchiveValidator.Validate(archiveStream);
        if (validationError is not null)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: validationError);
        }

        for (var attempt = 1; attempt <= MaxSaveAttempts; attempt++)
        {
            archiveStream.Position = 0;
            var snippetId = _idAllocator.NextCandidate(attempt);
            if (await _store.TryUploadAsync(BlobPath(snippetId), archiveStream, cancellationToken))
            {
                return Ok(EncodeSnippetId(snippetId));
            }
        }

        // Storage answered every time; we just did not find a free ID within budget. That is a retry for the
        // client, not a server fault, so say so instead of letting a storage exception become a 500.
        Response.Headers.RetryAfter = "1";
        return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, detail: "Could not allocate an ID for the snippet. Please try again.");
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
