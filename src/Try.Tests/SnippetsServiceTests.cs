namespace Tests
{
    using NUnit.Framework;
    using TryMudBlazor.Client.Services;
    using TryMudBlazor.Client.Models;
    using Try.Core;
    using Microsoft.Extensions.Options;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System;
    using static TryMudBlazor.Server.Utilities.SnippetsEncoder;

    /// <summary>
    /// Please note this is an integration test
    /// It needs blob storage backend to work
    /// You need an access key for the storage account in your secreets file
    /// under the server project do the following
    /// dotnet secrets init
    /// dotnet user-secrets set "SnippetsAccessKey" "yourlongkeyfromazureportal"
    /// </summary>

    public class Tests
    {
        private readonly List<CodeFile> codeFiles = new List<CodeFile>();
        private IOptions<SnippetsOptions> snippetsOptions;

        [SetUp]
        public void Setup()
        {
            var codeFile = new CodeFile() { Path = "__Main.razor" };
            codeFile.Content = "<MudDatePicker/>";
            codeFiles.Add(codeFile);
            var codeFile2 = new CodeFile() { Path = "Test.razor" };
            codeFile2.Content = "<h1>Test</h1>";
            codeFiles.Add(codeFile2);
            snippetsOptions = Options.Create(new SnippetsOptions(){SnippetsService = "api/snippets"});

        }


        [Test]
        [Ignore("Investigate failure")]
        public async Task TestGet()
        {
            var snippetService = new SnippetsService(snippetsOptions, new System.Net.Http.HttpClient(), new MockNavigationManager());
            var codeFiles = await snippetService.GetSnippetContentAsync("2021020540572059");
            Assert.That(codeFiles, Is.Not.Null);
        }

        [Test]
        [Ignore("Investigate failure")]
        public async Task TestPut()
        {
            var snippetService = new SnippetsService(snippetsOptions, new System.Net.Http.HttpClient(), new MockNavigationManager());
            var id = await snippetService.SaveSnippetAsync(codeFiles);
            Assert.That(id, Is.Not.Null);
            Console.WriteLine(id);
            var savedCodeFiles = await snippetService.GetSnippetContentAsync(id);
            List<CodeFile> savedCodeFilesList = new List<CodeFile>(savedCodeFiles);
            for (int i = 0; i  < codeFiles.Count; i++ )
            {
                Assert.That(codeFiles[i].Path, Is.EqualTo(savedCodeFilesList[i].Path));
                Assert.That(codeFiles[i].Content,  Is.EqualTo(savedCodeFilesList[i].Content));
            }
        }

        [Test]
        public void TestEncodeDecode()
        {
            const string snippetId = "2021020540572059";
            var encoded = EncodeSnippetId(snippetId);
            Console.WriteLine(encoded);
            var decoded = DecodeSnippetId(encoded);
            Console.WriteLine(decoded);
            Assert.That(snippetId, Is.EqualTo(decoded));
            var encoded2 = EncodeSnippetId(snippetId);
            Console.WriteLine(encoded2);
            Assert.That(encoded, Is.Not.EqualTo(encoded2));
        }
    }
}