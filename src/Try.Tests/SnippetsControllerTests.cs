#nullable enable

namespace Tests
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using NUnit.Framework;
    using Try.Core;
    using TryMudBlazor.Server.Controllers;
    using TryMudBlazor.Server.Utilities;
    using static TryMudBlazor.Server.Utilities.SnippetsEncoder;

    /// <summary>
    /// Drives the controller against an in-memory store and a frozen clock, so ID collisions, exhaustion,
    /// cancellation and concurrent saves are all deterministic.
    /// </summary>
    public class SnippetsControllerTests
    {
        // 12:00:00.123 UTC is 43,200,123 ms into the day.
        private static readonly DateTimeOffset Noon = new(2026, 9, 5, 12, 0, 0, 123, TimeSpan.Zero);
        private const string TimestampId = "2026090543200123";
        private const string TimestampPath = "2026/09/05/43200123";

        private static readonly byte[] ValidArchive = SnippetsServiceTests.Zip(
        [
            new CodeFile { Path = "__Main.razor", Content = "<MudText>hello</MudText>" },
        ]);

        [Test]
        public async Task FirstAttemptStoresUnderTheTimestampId()
        {
            var store = new FakeSnippetStore();

            var result = await CreateController(store).Post();

            Assert.That(DecodeSnippetId(SnippetIdOf(result)), Is.EqualTo(TimestampId));
            Assert.That(store.Paths, Is.EqualTo(new[] { TimestampPath }));
        }

        [Test]
        public async Task OneConflictRetriesWithADifferentId()
        {
            var store = new FakeSnippetStore { RejectFirst = 1 };

            var result = await CreateController(store).Post();

            Assert.That(store.Attempts, Has.Count.EqualTo(2));
            Assert.That(store.Attempts[0], Is.EqualTo(TimestampPath));
            Assert.That(store.Attempts[1], Is.Not.EqualTo(TimestampPath));
            Assert.That(DecodeSnippetId(SnippetIdOf(result)), Does.StartWith("20260905").And.Length.EqualTo(SnippetIdLength));
            Assert.That(store.Paths, Is.EqualTo(new[] { store.Attempts[1] }));
        }

        [Test]
        public async Task SeveralConflictsStillSucceedWithinTheBudget()
        {
            var store = new FakeSnippetStore { RejectFirst = SnippetsController.MaxSaveAttempts - 1 };

            var result = await CreateController(store).Post();

            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            Assert.That(store.Attempts, Has.Count.EqualTo(SnippetsController.MaxSaveAttempts));
            Assert.That(store.Attempts.Distinct().Count(), Is.EqualTo(store.Attempts.Count), "retries must not repeat a candidate");
        }

        [Test]
        public async Task ExhaustedBudgetIsAServiceUnavailableNotAServerError()
        {
            var store = new FakeSnippetStore { RejectFirst = int.MaxValue };
            var controller = CreateController(store);

            var result = await controller.Post();

            var problem = (ObjectResult)result;
            Assert.That(problem.StatusCode, Is.EqualTo(StatusCodes.Status503ServiceUnavailable));
            Assert.That(controller.Response.Headers.RetryAfter.ToString(), Is.EqualTo("1"));
            Assert.That(store.Attempts, Has.Count.EqualTo(SnippetsController.MaxSaveAttempts));
            Assert.That(store.Paths, Is.Empty);
        }

        [Test]
        public void CancelledRequestNeverReachesStorage()
        {
            var store = new FakeSnippetStore();
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            Assert.CatchAsync<OperationCanceledException>(() => CreateController(store, cancellation.Token).Post());
            Assert.That(store.Attempts, Is.Empty);
        }

        [Test]
        public void CancellationDuringUploadStopsTheRetries()
        {
            using var cancellation = new CancellationTokenSource();
            var store = new FakeSnippetStore { RejectFirst = int.MaxValue, OnUpload = cancellation.Cancel };

            Assert.CatchAsync<OperationCanceledException>(() => CreateController(store, cancellation.Token).Post());
            Assert.That(store.Attempts, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task ConcurrentSavesInTheSameMillisecondAllGetDistinctIds()
        {
            const int contenders = 25;
            var store = new FakeSnippetStore();

            var results = await Task.WhenAll(Enumerable.Range(0, contenders)
                .Select(seed => Task.Run(() => CreateController(store, seed: seed).Post())));

            var ids = results.Select(SnippetIdOf).Select(DecodeSnippetId).ToList();
            Assert.That(ids, Has.Count.EqualTo(contenders));
            Assert.That(ids.Distinct().Count(), Is.EqualTo(contenders));
            Assert.That(ids, Has.Exactly(1).EqualTo(TimestampId), "only one request can own the timestamp ID");
            Assert.That(store.Paths, Has.Count.EqualTo(contenders));
        }

        [Test]
        public async Task GetReturnsTheStoredArchive()
        {
            var store = new FakeSnippetStore();
            await CreateController(store).Post();

            var result = await CreateController(store).Get(EncodeSnippetId(TimestampId));

            var file = (FileStreamResult)result;
            using var downloaded = new MemoryStream();
            await file.FileStream.CopyToAsync(downloaded);
            Assert.That(downloaded.ToArray(), Is.EqualTo(ValidArchive));
        }

        [Test]
        public async Task GetReturnsNotFoundForAMissingSnippet()
        {
            var result = await CreateController(new FakeSnippetStore()).Get(EncodeSnippetId(TimestampId));

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        private static string SnippetIdOf(IActionResult result) => (string)((OkObjectResult)result).Value!;

        private static SnippetsController CreateController(ISnippetStore store, CancellationToken cancellationToken = default, int seed = 1)
        {
            var httpContext = new DefaultHttpContext { RequestAborted = cancellationToken };
            httpContext.Request.Body = new MemoryStream(ValidArchive);

            // A Random per controller: Random.Shared is thread-safe but a seeded instance is not.
            var allocator = new SnippetIdAllocator(new FrozenClock(Noon), new Random(seed));
            return new SnippetsController(store, allocator)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
            };
        }

        private sealed class FrozenClock(DateTimeOffset now) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => now;
        }

        private sealed class FakeSnippetStore : ISnippetStore
        {
            private readonly ConcurrentDictionary<string, byte[]> _blobs = new();
            private readonly ConcurrentQueue<string> _attempts = new();
            private int _uploads;

            /// <summary>How many uploads to answer with "already exists" before behaving normally.</summary>
            public int RejectFirst { get; init; }

            /// <summary>Runs on every upload after the attempt is recorded and before the cancellation token is checked.</summary>
            public Action? OnUpload { get; init; }

            public IReadOnlyList<string> Attempts => _attempts.ToArray();

            public IReadOnlyCollection<string> Paths => _blobs.Keys.ToArray();

            public Task<Stream?> DownloadAsync(string blobPath, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult<Stream?>(_blobs.TryGetValue(blobPath, out var bytes) ? new MemoryStream(bytes) : null);
            }

            public Task<bool> TryUploadAsync(string blobPath, Stream content, CancellationToken cancellationToken)
            {
                _attempts.Enqueue(blobPath);
                OnUpload?.Invoke();
                cancellationToken.ThrowIfCancellationRequested();

                if (Interlocked.Increment(ref _uploads) <= RejectFirst)
                {
                    return Task.FromResult(false);
                }

                using var buffer = new MemoryStream();
                content.CopyTo(buffer);
                return Task.FromResult(_blobs.TryAdd(blobPath, buffer.ToArray()));
            }
        }
    }
}
