namespace TryMudBlazor.Client.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using MudBlazor;
    using Try.Core;
    using TryMudBlazor.Client.Services;

    public partial class SaveSnippetPopup
    {
        [Inject]
        public ISnackbar Snackbar { get; set; }

        [Inject]
        protected IJsApiService JsApiService { get; set; }

        [Inject]
        public SnippetsService SnippetsService { get; set; }

        [Inject]
        public NavigationManager NavigationManager { get; set; }

        [Parameter]
        public bool Visible { get; set; }

        [Parameter]
        public EventCallback<bool> VisibleChanged { get; set; }

        [Parameter]
        public IEnumerable<CodeFile> CodeFiles { get; set; } = Enumerable.Empty<CodeFile>();

        [Parameter]
        public Action UpdateActiveCodeFileContentAction { get; set; }

        private bool Loading { get; set; }
        private string SnippetLink { get; set; }

        private async Task CopyLinkToClipboard()
        {
            await JsApiService.CopyToClipboardAsync(SnippetLink);
        }

        private async Task SaveAsync()
        {
            Loading = true;

            try
            {
                this.UpdateActiveCodeFileContentAction?.Invoke();

                var snippetId = await this.SnippetsService.SaveSnippetAsync(this.CodeFiles);
                var urlBuilder = new UriBuilder(this.NavigationManager.BaseUri) { Path = $"snippet/{snippetId}" };
                this.SnippetLink = urlBuilder.Uri.ToString();
                // Same page component, so this only updates the address bar and NavigationManager.Uri; a later reload (e.g. Clear cache) then comes back to the saved snippet.
                this.NavigationManager.NavigateTo(this.SnippetLink, replace: true);
            }
            catch (InvalidOperationException ex)
            {
                Snackbar.Add(ex.Message, Severity.Error);
            }
            catch (HttpRequestException)
            {
                Snackbar.Add("Could not save the snippet. Please try again later.", Severity.Error);
            }
            catch (Exception)
            {
                Snackbar.Add("Error while saving snippet. Please try again later.", Severity.Error);
            }
            finally
            {
                Loading = false;
            }
        }

        private async Task OnClose()
        {
            Loading = false;
            SnippetLink = string.Empty;

            await VisibleChanged.InvokeAsync(false);
        }
    }
}
