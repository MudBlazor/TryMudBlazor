namespace Try.Core
{
    public static class CoreConstants
    {
        public const string MainComponentFilePath = "__Main.razor";
        public const string MainComponentDefaultFileContent = @"
<MudText Typo=""Typo.h6"">MudBlazor is @Text</MudText>
<MudButton Variant=""Variant.Filled"" Color=""Color.Primary"" OnClick=""ButtonOnClick"">@ButtonText</MudButton>

@code {
    public string Text { get; set; } = ""????"";
    public string ButtonText { get; set; } = ""Click Me"";
    public int ButtonClicked { get; set; }

    void ButtonOnClick()
    {
        ButtonClicked += 1;
        Text = $""Awesome x {ButtonClicked}"";
        ButtonText = ""Click Me Again"";
    }
}";

        public const string DefaultRazorFileContentFormat = "<h1>{0}</h1>";

        public static readonly string DefaultCSharpFileContentFormat =
            @$"namespace {CompilationService.DefaultRootNamespace}
{{{{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class {{0}}
    {{{{
    }}}}
}}}}
";
    }
}
