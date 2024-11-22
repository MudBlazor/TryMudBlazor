namespace TryMudBlazor.Client.Models
{
    public static class Try
    {
        public const string Initialize = "Try.initialize";
        public const string ChangeDisplayUrl = "Try.changeDisplayUrl";
        public const string ReloadIframe = "Try.reloadIframe";
        public const string Dispose = "Try.dispose";
        public static class Editor
        {
            public const string Create = "Try.Editor.create";
            public const string GetValue = "Try.Editor.getValue";
            public const string SetValue = "Try.Editor.setValue";
            public const string SetLangugage = "Try.Editor.setLanguage";
            public const string Focus = "Try.Editor.focus";
            public const string SetTheme = "Try.Editor.setTheme";
            public const string Dispose = "Try.Editor.dispose";
        }

        public static class CodeExecution
        {
            public const string GetCompilationDlls = "Try.CodeExecution.getCompilationDlls";
            public const string UpdateUserComponentsDll = "Try.CodeExecution.updateUserComponentsDll";
        }
    }
}
