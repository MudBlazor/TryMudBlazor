namespace Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using NUnit.Framework;
    using Try.Core;

    /// <summary>
    /// Compiles small snippets through the real <see cref="CompilationService"/> and checks that
    /// diagnostics point at the same 1-based line the user sees in the editor.
    /// </summary>
    public class CompilationDiagnosticTests
    {
        private CompilationService compilationService;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            await CompilationService.InitAsync();
            compilationService = new CompilationService();
        }

        [Test]
        public async Task CSharpErrorInMainComponentReportsEditorLine()
        {
            // Leading blank line on purpose: the default template starts with one and it must count.
            const string main = "\n<MudText>hi</MudText>\n\n@code {\n    void Go()\n    {\n        int x = \"not an int\";\n    }\n}\n";

            var result = await Compile(main);

            var error = result.Diagnostics.Single(d => d.Code == "CS0029");
            Assert.That(error.File, Is.EqualTo(CoreConstants.MainComponentFilePath));
            Assert.That(error.Line, Is.EqualTo(7));
        }

        [Test]
        public async Task CSharpErrorInSecondaryComponentReportsEditorLine()
        {
            const string main = "<Second />";
            const string second = "<h1>Second</h1>\n@code {\n    int y = \"bad\";\n}\n";

            var result = await Compile(main, ("Second.razor", second));

            var error = result.Diagnostics.Single(d => d.Code == "CS0029");
            Assert.That(error.File, Is.EqualTo("Second.razor"));
            Assert.That(error.Line, Is.EqualTo(3));
        }

        [Test]
        public async Task RazorErrorReportsEditorLine()
        {
            const string main = "<MudText>ok</MudText>\n<MudAlert>unclosed\n";

            var result = await Compile(main);

            var error = result.Diagnostics.First(d => d.Kind == CompilationDiagnosticKind.Razor && d.Severity == DiagnosticSeverity.Error);
            Assert.That(error.File, Is.EqualTo(CoreConstants.MainComponentFilePath));
            Assert.That(error.Line, Is.EqualTo(2));
        }

        [Test]
        public async Task ValidSnippetProducesAssembly()
        {
            var result = await Compile(CoreConstants.MainComponentDefaultFileContent);

            Assert.That(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error), Is.Empty);
            Assert.That(result.AssemblyBytes, Is.Not.Null.And.Not.Empty);
        }

        private Task<CompileToAssemblyResult> Compile(string mainContent, params (string path, string content)[] extraFiles)
        {
            var files = new List<CodeFile> { new() { Path = CoreConstants.MainComponentFilePath, Content = mainContent } };
            files.AddRange(extraFiles.Select(f => new CodeFile { Path = f.path, Content = f.content }));
            return compilationService.CompileToAssemblyAsync(files, updateStatusFunc: null);
        }
    }
}
