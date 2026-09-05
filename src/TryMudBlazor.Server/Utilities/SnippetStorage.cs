using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;

namespace TryMudBlazor.Server.Utilities;

public static class SnippetStorage
{
    /// <summary>
    /// Builds the container client once at startup. Creating it per request would also create a new
    /// <see cref="DefaultAzureCredential"/> each time, and with it a fresh token acquisition on every save or load.
    /// </summary>
    public static BlobContainerClient CreateContainerClient(IConfiguration config)
    {
        var snippetsContainerUrl = config["SnippetsContainerUrl"];
        var accessKey = config["SnippetsAccessKey"];

        if (string.IsNullOrEmpty(snippetsContainerUrl) || string.IsNullOrEmpty(accessKey))
        {
            throw new InvalidOperationException("Please configure SnippetsContainerUrl and SnippetsAccessKey in appsettings.json");
        }

        var containerUri = new Uri(snippetsContainerUrl);

        if (accessKey == "secret")
        {
            var defaultAzureCredentialOptions = new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = config["ManagedCredentialsId"]
            };
            return new BlobContainerClient(containerUri, new DefaultAzureCredential(defaultAzureCredentialOptions));
        }

        var accountName = new BlobUriBuilder(containerUri).AccountName;
        return new BlobContainerClient(containerUri, new StorageSharedKeyCredential(accountName, accessKey));
    }
}
