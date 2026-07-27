namespace TryMudBlazor.Client.Shared
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using MudBlazor;
    using Services;
    using Try.Core;

    public partial class MainLayout : LayoutComponentBase, IDisposable
    {
        private MudThemeProvider _mudThemeProvider;

        [Inject]
        private LayoutService LayoutService { get; set; }

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
                await CompilationService.InitAsync();
                StateHasChanged();
            }
        }

        private async Task ApplyUserPreferences()
        {
            var defaultDarkMode = await _mudThemeProvider.GetSystemDarkModeAsync();
            await LayoutService.ApplyUserPreferences(defaultDarkMode);
        }

        public void Dispose()
        {
            LayoutService.MajorUpdateOccured -= LayoutServiceOnMajorUpdateOccured;
        }

        private void LayoutServiceOnMajorUpdateOccured(object sender, EventArgs e) => StateHasChanged();
    }
}
