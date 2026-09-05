namespace Tests
{
    using System.IO;
    using System.Linq;
    using System.Text;
    using NUnit.Framework;
    using Try.Core;
    using TryMudBlazor.Server.Utilities;

    public class SnippetArchiveValidatorTests
    {
        [Test]
        public void AcceptsAValidSnippetArchive()
        {
            var archive = SnippetsServiceTests.Zip(
            [
                new CodeFile { Path = "__Main.razor", Content = "<MudText>hello</MudText>" },
                new CodeFile { Path = "Helper.cs", Content = "public class Helper { }" },
            ]);

            Assert.That(Validate(archive), Is.Null);
        }

        [Test]
        public void RejectsBodiesThatAreNotZipArchives()
        {
            Assert.That(Validate(Encoding.UTF8.GetBytes("plain text, not a zip")), Is.EqualTo("The snippet must be a zip archive."));
        }

        [Test]
        public void RejectsArchivesWithoutTheMainComponent()
        {
            var archive = SnippetsServiceTests.Zip([new CodeFile { Path = "Other.razor", Content = "<h1>x</h1>" }]);

            Assert.That(Validate(archive), Is.EqualTo("No main component file provided."));
        }

        [Test]
        public void RejectsEntriesWithForeignExtensions()
        {
            var archive = SnippetsServiceTests.Zip(
            [
                new CodeFile { Path = "__Main.razor", Content = "<MudText>hello</MudText>" },
                new CodeFile { Path = "payload.exe", Content = "MZ" },
            ]);

            Assert.That(Validate(archive), Does.StartWith("File 'payload.exe' has invalid extension"));
        }

        [Test]
        public void RejectsTooManyFiles()
        {
            var files = Enumerable.Range(0, SnippetArchiveValidator.MaxFiles + 1)
                .Select(i => new CodeFile { Path = i == 0 ? "__Main.razor" : $"C{i}.razor", Content = "<h1>x</h1>" });

            Assert.That(Validate(SnippetsServiceTests.Zip(files)), Is.EqualTo($"A snippet can contain at most {SnippetArchiveValidator.MaxFiles} files."));
        }

        [Test]
        public void RejectsOversizedFiles()
        {
            var archive = SnippetsServiceTests.Zip(
            [
                new CodeFile { Path = "__Main.razor", Content = new string('x', SnippetArchiveValidator.MaxFileBytes + 1) },
            ]);

            Assert.That(Validate(archive), Does.StartWith("File '__Main.razor' is larger than"));
        }

        private static string Validate(byte[] body)
        {
            using var stream = new MemoryStream(body);
            return SnippetArchiveValidator.Validate(stream);
        }
    }
}
