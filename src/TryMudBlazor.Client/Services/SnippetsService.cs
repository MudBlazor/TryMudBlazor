namespace TryMudBlazor.Client.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.Extensions.Options;
    using Try.Core;
    using TryMudBlazor.Client.Models;

    public class SnippetsService
    {
        private const int SnippetIdLength = 16;

        private readonly HttpClient httpClient;
        private readonly string snippetsService;

        public SnippetsService(IOptions<SnippetsOptions> snippetsOptions, HttpClient httpClient, NavigationManager navigationManager)
        {
            this.httpClient = httpClient;
            this.snippetsService = $"{navigationManager.BaseUri}{snippetsOptions.Value.SnippetsService}";
        }

        /// <summary>
        /// Uploads the files and returns the public snippet ID.
        /// </summary>
        /// <exception cref="InvalidOperationException">The files fail validation or the server rejected them.</exception>
        /// <exception cref="HttpRequestException">The server could not be reached or returned an error.</exception>
        public async Task<string> SaveSnippetAsync(IEnumerable<CodeFile> codeFiles)
        {
            if (codeFiles == null)
            {
                throw new ArgumentNullException(nameof(codeFiles));
            }

            var codeFilesValidationError = CodeFilesHelper.ValidateCodeFilesForSnippetCreation(codeFiles);
            if (!string.IsNullOrWhiteSpace(codeFilesValidationError))
            {
                throw new InvalidOperationException(codeFilesValidationError);
            }

            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var codeFile in codeFiles)
                {
                    var byteArray = Encoding.UTF8.GetBytes(codeFile.Content);
                    var codeEntry = archive.CreateEntry(codeFile.Path);
                    using var entryStream = codeEntry.Open();
                    entryStream.Write(byteArray);
                }
            }

            memoryStream.Position = 0;

            var inputData = new StreamContent(memoryStream);

            using var response = await this.httpClient.PostAsync(this.snippetsService, inputData);
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                // The server ran the same validation and explains what it rejected.
                var reason = await ReadProblemDetailAsync(response);
                throw new InvalidOperationException(reason ?? "The server rejected the snippet.");
            }

            response.EnsureSuccessStatusCode();

            var snippetId = await response.Content.ReadAsStringAsync();
            if (!IsServerSnippetId(snippetId))
            {
                // Anything else is an error page or a proxy response, never a snippet link.
                throw new HttpRequestException("The server did not return a snippet ID.");
            }

            return snippetId;
        }

        /// <summary>
        /// Loads a snippet either from the server by its ID or from the compressed form embedded in the URL.
        /// </summary>
        /// <exception cref="ArgumentException">The ID is malformed or no snippet exists for it.</exception>
        /// <exception cref="HttpRequestException">The server could not be reached or returned an error.</exception>
        public async Task<IEnumerable<CodeFile>> GetSnippetContentAsync(string snippetId)
        {
            if (string.IsNullOrWhiteSpace(snippetId))
            {
                throw new ArgumentException("Invalid snippet ID.", nameof(snippetId));
            }

            if (snippetId.Length != SnippetIdLength)
            {
                try
                {
                    var snippetFiles = snippetId.ToCodeFiles();
                    var codeFilesValidationError = CodeFilesHelper.ValidateCodeFilesForSnippetCreation(snippetFiles);
                    if (!string.IsNullOrWhiteSpace(codeFilesValidationError))
                    {
                        throw new InvalidOperationException(codeFilesValidationError);
                    }

                    return snippetFiles;
                }
                catch
                {
                    throw new ArgumentException("Invalid snippet ID.", nameof(snippetId));
                }
            }

            using var response = await this.httpClient.GetAsync($"{this.snippetsService}/{snippetId}");
            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
            {
                throw new ArgumentException("Invalid snippet ID.", nameof(snippetId));
            }

            response.EnsureSuccessStatusCode();

            using var zipStream = new MemoryStream(await response.Content.ReadAsByteArrayAsync());
            return await ExtractSnippetFilesFromZip(zipStream);
        }

        private static bool IsServerSnippetId(string snippetId) =>
            snippetId?.Length == SnippetIdLength && snippetId.All(char.IsAsciiLetter);

        private static async Task<string> ReadProblemDetailAsync(HttpResponseMessage response)
        {
            try
            {
                var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>();
                return string.IsNullOrWhiteSpace(problem?.Detail) ? null : problem.Detail;
            }
            catch
            {
                return null;
            }
        }

        private static async Task<IEnumerable<CodeFile>> ExtractSnippetFilesFromZip(Stream zipStream)
        {
            var result = new List<CodeFile>();

            using var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Read);
            foreach (var entry in zipArchive.Entries)
            {
                using var streamReader = new StreamReader(entry.Open());
                result.Add(new CodeFile { Path = entry.FullName, Content = await streamReader.ReadToEndAsync() });
            }

            return result;
        }

        private sealed class ProblemPayload
        {
            public string Detail { get; set; }
        }
    }
}
