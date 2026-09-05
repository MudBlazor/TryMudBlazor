namespace Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Options;
    using NUnit.Framework;
    using Try.Core;
    using TryMudBlazor.Client.Models;
    using TryMudBlazor.Client.Services;
    using static TryMudBlazor.Server.Utilities.SnippetsEncoder;

    public class SnippetsServiceTests
    {
        private const string ValidSnippetId = "cacbacaeeafhcafj"; // 16 letters, like the server produces

        private List<CodeFile> codeFiles;

        [SetUp]
        public void Setup()
        {
            codeFiles =
            [
                new CodeFile { Path = "__Main.razor", Content = "<MudDatePicker/>" },
                new CodeFile { Path = "Test.razor", Content = "<h1>Test</h1>" },
            ];
        }

        [Test]
        public async Task SaveReturnsTheIdTheServerProduced()
        {
            var handler = new FakeHandler(_ => Text(HttpStatusCode.OK, ValidSnippetId));
            var service = CreateService(handler);

            var id = await service.SaveSnippetAsync(codeFiles);

            Assert.That(id, Is.EqualTo(ValidSnippetId));
            Assert.That(handler.Requests.Single().Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(handler.Requests.Single().RequestUri!.ToString(), Is.EqualTo("https://localhost:5001/api/snippets"));
        }

        [Test]
        public void SaveThrowsOnServerError()
        {
            var service = CreateService(new FakeHandler(_ => Text(HttpStatusCode.InternalServerError, "Azure.Identity.CredentialUnavailableException: ...")));

            Assert.ThrowsAsync<HttpRequestException>(() => service.SaveSnippetAsync(codeFiles));
        }

        [Test]
        public void SaveThrowsWhenASuccessResponseIsNotASnippetId()
        {
            var service = CreateService(new FakeHandler(_ => Text(HttpStatusCode.OK, "<!DOCTYPE html><html>...")));

            Assert.ThrowsAsync<HttpRequestException>(() => service.SaveSnippetAsync(codeFiles));
        }

        [Test]
        public void SaveSurfacesTheServersValidationMessage()
        {
            var service = CreateService(new FakeHandler(_ => Json(HttpStatusCode.BadRequest, "{\"title\":\"Bad Request\",\"status\":400,\"detail\":\"A snippet can contain at most 20 files.\"}")));

            var exception = Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveSnippetAsync(codeFiles));
            Assert.That(exception!.Message, Is.EqualTo("A snippet can contain at most 20 files."));
        }

        [Test]
        public void SaveRejectsInvalidFilesBeforeCallingTheServer()
        {
            var handler = new FakeHandler(_ => Text(HttpStatusCode.OK, ValidSnippetId));
            var service = CreateService(handler);

            Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveSnippetAsync([new CodeFile { Path = "Test.razor", Content = "<h1/>" }]));
            Assert.That(handler.Requests, Is.Empty);
        }

        [Test]
        public async Task GetReturnsTheFilesFromTheServerArchive()
        {
            var service = CreateService(new FakeHandler(_ => Bytes(HttpStatusCode.OK, Zip(codeFiles))));

            var files = (await service.GetSnippetContentAsync(ValidSnippetId)).ToList();

            Assert.That(files.Select(f => f.Path), Is.EqualTo(codeFiles.Select(f => f.Path)));
            Assert.That(files.Select(f => f.Content), Is.EqualTo(codeFiles.Select(f => f.Content)));
        }

        [TestCase(HttpStatusCode.NotFound)]
        [TestCase(HttpStatusCode.BadRequest)]
        public void GetTreatsAMissingSnippetAsAnInvalidId(HttpStatusCode statusCode)
        {
            var service = CreateService(new FakeHandler(_ => Text(statusCode, string.Empty)));

            Assert.ThrowsAsync<ArgumentException>(() => service.GetSnippetContentAsync(ValidSnippetId));
        }

        [Test]
        public void GetThrowsOnServerError()
        {
            var service = CreateService(new FakeHandler(_ => Text(HttpStatusCode.InternalServerError, string.Empty)));

            Assert.ThrowsAsync<HttpRequestException>(() => service.GetSnippetContentAsync(ValidSnippetId));
        }

        [TestCase("a")]
        [TestCase("")]
        [TestCase("cacbacaeeafhcaf")]
        [TestCase("cacbacaeeafhcafjj")]
        [TestCase("cacbacaeeafhcaf1")]
        [TestCase("cacbacaeeafhca-j")]
        public void DecodeRejectsIdsOfTheWrongShape(string encoded)
        {
            Assert.Throws<InvalidDataException>(() => DecodeSnippetId(encoded));
        }

        [Test]
        public void TestEncodeDecode()
        {
            const string snippetId = "2021020540572059";
            var encoded = EncodeSnippetId(snippetId);
            var decoded = DecodeSnippetId(encoded);
            Assert.That(snippetId, Is.EqualTo(decoded));
            var encoded2 = EncodeSnippetId(snippetId);
            Assert.That(encoded, Is.Not.EqualTo(encoded2));
        }

        private static SnippetsService CreateService(HttpMessageHandler handler)
        {
            var options = Options.Create(new SnippetsOptions { SnippetsService = "api/snippets" });
            return new SnippetsService(options, new HttpClient(handler), new MockNavigationManager());
        }

        internal static byte[] Zip(IEnumerable<CodeFile> files)
        {
            using var stream = new MemoryStream();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var file in files)
                {
                    using var entry = archive.CreateEntry(file.Path).Open();
                    entry.Write(Encoding.UTF8.GetBytes(file.Content));
                }
            }

            return stream.ToArray();
        }

        private static HttpResponseMessage Text(HttpStatusCode status, string body) =>
            new(status) { Content = new StringContent(body, Encoding.UTF8, "text/plain") };

        private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
            new(status) { Content = new StringContent(body, Encoding.UTF8, "application/problem+json") };

        private static HttpResponseMessage Bytes(HttpStatusCode status, byte[] body) =>
            new(status) { Content = new ByteArrayContent(body) };

        private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
        {
            public List<HttpRequestMessage> Requests { get; } = [];

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Requests.Add(request);
                return Task.FromResult(respond(request));
            }
        }
    }
}
