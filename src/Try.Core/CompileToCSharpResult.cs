namespace Try.Core
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Razor.Language;

    internal class CompileToCSharpResult
    {
        public RazorProjectItem ProjectItem { get; set; }

        public string Code { get; set; }

        public string FilePath { get; set; }

        public IEnumerable<CompilationDiagnostic> Diagnostics { get; set; } = [];
    }
}
