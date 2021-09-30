namespace TryMudBlazor.Client.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using TryMudBlazor.Client.Services;
    using Try.Core;
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;
    using MudBlazor;

    public partial class SaveSnippetPopup : IDisposable
    {
        private DotNetObjectReference<SaveSnippetPopup> dotNetInstance;

        [Inject]
        public ISnackbar Snackbar { get; set; }

        [Inject]
        public IJSInProcessRuntime JsRuntime { get; set; }

        [Inject]
        public SnippetsService SnippetsService { get; set; }

        [Inject]
        public NavigationManager NavigationManager { get; set; }

        [Parameter]
        public bool Visible { get; set; }

        [Parameter]
        public EventCallback<bool> VisibleChanged { get; set; }

        [Parameter]
        public string InvokerId { get; set; }

        [Parameter]
        public IEnumerable<CodeFile> CodeFiles { get; set; } = Enumerable.Empty<CodeFile>();

        [Parameter]
        public Action UpdateActiveCodeFileContentAction { get; set; }

        private bool Loading { get; set; }

        private string SnippetLink { get; set; }

        private bool SnippetLinkCopied { get; set; }

        private string VisibleClass => this.Visible ? "show" : string.Empty;

        private string CopyButtonIcon => this.SnippetLinkCopied ? "icon-check" : "icon-copy";

        private string DisplayStyle => this.Visible ? string.Empty : "display: none;";

        public void Dispose()
        {
            this.dotNetInstance?.Dispose();

            this.JsRuntime.InvokeVoid("App.SaveSnippetPopup.dispose");
        }

        [JSInvokable]
        public Task CloseAsync() => this.CloseInternalAsync();

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                this.dotNetInstance = DotNetObjectReference.Create(this);

                this.JsRuntime.InvokeVoid(
                    "App.SaveSnippetPopup.init",
                    "save-snippet-popup",
                    this.InvokerId,
                    this.dotNetInstance);
            }

            base.OnAfterRender(firstRender);
        }

        private void CopyLinkToClipboard()
        {
            this.JsRuntime.InvokeVoid("App.copyToClipboard", this.SnippetLink);
            this.SnippetLinkCopied = true;
        }

        private async Task SaveAsync()
        {
            this.Loading = true;

            try
            {
                this.UpdateActiveCodeFileContentAction?.Invoke();

                var snippetId = await this.SnippetsService.SaveSnippetAsync(this.CodeFiles);

                var urlBuilder = new UriBuilder(this.NavigationManager.BaseUri) { Path = $"snippet/{snippetId}" };
                var url = urlBuilder.Uri.ToString();
                this.SnippetLink = url;

                this.JsRuntime.InvokeVoid("App.changeDisplayUrl", url);
            }
            catch (InvalidOperationException ex)
            {
                Snackbar.Add(ex.Message, Severity.Error);
            }
            catch (Exception)
            {
                Snackbar.Add("Error while saving snippet. Please try again later.", Severity.Error);
            }
            finally
            {
                this.Loading = false;
            }
        }

        private Task CloseInternalAsync()
        {
            this.Visible = false;
            this.SnippetLink = null;
            this.SnippetLinkCopied = false;
            return this.VisibleChanged.InvokeAsync(this.Visible);
        }
    }
}
