namespace Try.Core
{
    using System.IO;

    public class SavedSnippet
    {
        public string SnippetId { get; set; }

        public Stream ZippedStream { get; set; }
    }
}