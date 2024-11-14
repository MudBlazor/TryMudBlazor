namespace Try.Core
{
    using System;
    using System.Text.Json.Serialization;

    public class CodeFile
    {
        public const string RazorFileExtension = ".razor";
        public const string CsharpFileExtension = ".cs";

        private CodeFileType? type;

        public string Path { get; init; }

        public string Content { get; set; }

        [JsonIgnore]
        public CodeFileType Type
        {
            get
            {
                if (!type.HasValue)
                {
                    var extension = System.IO.Path.GetExtension(Path);

                    type = extension switch
                    {
                        RazorFileExtension => CodeFileType.Razor,
                        CsharpFileExtension => CodeFileType.CSharp,
                        _ => throw new NotSupportedException($"Unsupported extension: {extension}"),
                    };
                }

                return type.Value;
            }
        }
    }
}
