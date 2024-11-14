namespace Try.Core
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.AspNetCore.Razor.Language;

    internal class VirtualRazorProjectFileSystem : RazorProjectFileSystem
    {
        public override IEnumerable<RazorProjectItem> EnumerateItems(string basePath)
        {
            NormalizeAndEnsureValidPath(basePath);
            return Enumerable.Empty<RazorProjectItem>();
        }

        public override RazorProjectItem GetItem(string path) => GetItem(path, fileKind: null);

        public override RazorProjectItem GetItem(string path, string fileKind)
        {
            NormalizeAndEnsureValidPath(path);
            return new NotFoundProjectItem(string.Empty, path);
        }
    }
}
