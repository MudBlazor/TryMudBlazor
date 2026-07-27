namespace TryMudBlazor.Client.Services.UserPreferences;

using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;

public interface IUserPreferencesService
{
    /// <summary>
    /// Saves UserPreferences in local storage
    /// </summary>
    /// <param name="userPreferences">The userPreferences to save in the local storage</param>
    public Task SaveUserPreferences(UserPreferences userPreferences);

    /// <summary>
    /// Loads UserPreferences in local storage
    /// </summary>
    /// <returns>UserPreferences object. Null when no settings were found.</returns>
    public Task<UserPreferences> LoadUserPreferences();
}

public class UserPreferencesService : IUserPreferencesService
{
    private readonly IJSRuntime _jsRuntime;
    private const string Key = "userPreferences";

    public UserPreferencesService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task SaveUserPreferences(UserPreferences userPreferences)
    {
        // Default serializer options keep the PascalCase property names that editor/main.js reads directly.
        var json = JsonSerializer.Serialize(userPreferences);

        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", Key, json);
    }

    public async Task<UserPreferences> LoadUserPreferences()
    {
        var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", Key);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<UserPreferences>(json);
        }
        catch (JsonException)
        {
            // Ignore preferences that can no longer be read and fall back to the defaults.
            return null;
        }
    }
}
