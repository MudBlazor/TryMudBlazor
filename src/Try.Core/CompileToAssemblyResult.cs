namespace Try.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;

    public class CompileToAssemblyResult
    {
        public Compilation Compilation { get; set; }

        public IEnumerable<CompilationDiagnostic> Diagnostics { get; set; } = [];

        public byte[] AssemblyBytes { get; set; }
    }
}
