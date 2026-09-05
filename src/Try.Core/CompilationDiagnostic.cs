namespace Try.Core
{
    using System.IO;
    using Microsoft.AspNetCore.Razor.Language;
    using Microsoft.CodeAnalysis;

    public class CompilationDiagnostic
    {
        public string Code { get; set; }

        public DiagnosticSeverity Severity { get; set; }

        public string Description { get; set; }

        /// <summary>
        /// 1-based line in the user's source file, matching the editor gutter.
        /// </summary>
        public int? Line { get; set; }

        public string File { get; set; }

        public CompilationDiagnosticKind Kind { get; set; }

        internal static CompilationDiagnostic FromCSharpDiagnostic(Diagnostic diagnostic)
        {
            if (diagnostic == null)
            {
                return null;
            }

            // The generated C# carries #line directives back to the .razor source, so the mapped
            // span already points at the user's file. Roslyn lines are 0-based; the editor is 1-based.
            var mappedLineSpan = diagnostic.Location.GetMappedLineSpan();

            return new CompilationDiagnostic
            {
                Kind = CompilationDiagnosticKind.CSharp,
                Code = diagnostic.Descriptor.Id,
                Severity = diagnostic.Severity,
                Description = diagnostic.GetMessage(),
                File = Path.GetFileName(mappedLineSpan.Path),
                Line = mappedLineSpan.StartLinePosition.Line + 1,
            };
        }

        internal static CompilationDiagnostic FromRazorDiagnostic(RazorDiagnostic diagnostic)
        {
            if (diagnostic == null)
            {
                return null;
            }

            return new CompilationDiagnostic
            {
                Kind = CompilationDiagnosticKind.Razor,
                Code = diagnostic.Id,
                Severity = (DiagnosticSeverity)diagnostic.Severity,
                Description = diagnostic.GetMessage(),
                File = Path.GetFileName(diagnostic.Span.FilePath),
                Line = diagnostic.Span.LineIndex + 1,
            };
        }
    }
}
