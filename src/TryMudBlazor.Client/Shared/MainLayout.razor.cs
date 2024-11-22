namespace TryMudBlazor.Client.Shared
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;
    using MudBlazor;
    using Services;
    using Try.Core;
    using static TryMudBlazor.Client.Models.Try;

    public partial class MainLayout : LayoutComponentBase, IDisposable
    {
        private MudThemeProvider _mudThemeProvider;

        [Inject]
        private LayoutService LayoutService { get; set; }

        [Inject]
        private IJSRuntime JsRuntime { get; set; }

        protected override void OnInitialized()
        {
            LayoutService.MajorUpdateOccured += LayoutServiceOnMajorUpdateOccured;
            base.OnInitialized();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (firstRender)
            {
                await ApplyUserPreferences();
                await CompilationService.InitAsync(GetReferenceAssembliesStreamsAsync);
                StateHasChanged();
            }
        }

        private ValueTask<IReadOnlyList<byte[]>> GetReferenceAssembliesStreamsAsync(IEnumerable<string> referenceAssemblyNames)
        {
            return JsRuntime.InvokeAsync<IReadOnlyList<byte[]>>(CodeExecution.GetCompilationDlls, new List<string>(referenceAssemblyNames) { "netstandard" });
        }

        private async Task ApplyUserPreferences()
        {
            var defaultDarkMode = await _mudThemeProvider.GetSystemPreference();
            await LayoutService.ApplyUserPreferences(defaultDarkMode);
        }

        public void Dispose()
        {
            LayoutService.MajorUpdateOccured -= LayoutServiceOnMajorUpdateOccured;
        }

        private void LayoutServiceOnMajorUpdateOccured(object sender, EventArgs e) => StateHasChanged();
    }
}
