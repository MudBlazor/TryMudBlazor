namespace TryMudBlazor.Client.Shared
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Try.Core;
    using Microsoft.AspNetCore.Components;

    public partial class MainLayout
    {
        [Inject]
        public HttpClient HttpClient { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await CompilationService.InitAsync(this.HttpClient);

            await base.OnInitializedAsync();
        }
    }
}
