namespace TryMudBlazor.Client.Services;

using System;
using System.Threading.Tasks;
using UserPreferences;

public class LayoutService
{
    private readonly IUserPreferencesService _userPreferencesService;
    private UserPreferences.UserPreferences _userPreferences;
    private Task _initialization;

    public bool IsDarkMode { get; private set; } = false;

    public LayoutService(IUserPreferencesService userPreferencesService)
    {
        _userPreferencesService = userPreferencesService;
    }

    public void SetDarkMode(bool value)
    {
        IsDarkMode = value;
    }

    /// <summary>
    /// Loads the stored preferences once per app lifetime. Every layout awaits the same load, and a load that
    /// finishes after the user has already toggled the theme leaves that choice alone (see <see cref="ToggleDarkMode"/>).
    /// </summary>
    public Task ApplyUserPreferences(bool isDarkModeDefaultTheme)
    {
        return _initialization ??= LoadUserPreferencesAsync(isDarkModeDefaultTheme);
    }

    private async Task LoadUserPreferencesAsync(bool isDarkModeDefaultTheme)
    {
        var storedPreferences = await _userPreferencesService.LoadUserPreferences();

        if (_userPreferences != null)
        {
            // The user toggled the theme while the load was in flight. That choice is newer than anything
            // in storage, and ToggleDarkMode has already saved it, so the older value must not replace it.
            return;
        }

        if (storedPreferences != null)
        {
            _userPreferences = storedPreferences;
            IsDarkMode = storedPreferences.DarkTheme;
        }
        else
        {
            IsDarkMode = isDarkModeDefaultTheme;
            _userPreferences = new UserPreferences.UserPreferences { DarkTheme = IsDarkMode };
            await _userPreferencesService.SaveUserPreferences(_userPreferences);
        }
    }

    public event EventHandler MajorUpdateOccured;

    private void OnMajorUpdateOccured() => MajorUpdateOccured?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Flips the theme relative to what the user currently sees and persists it. Works before, during and after
    /// the initial load; a load still in flight will not overwrite the result.
    /// </summary>
    public async Task ToggleDarkMode()
    {
        IsDarkMode = !IsDarkMode;
        _userPreferences ??= new UserPreferences.UserPreferences();
        _userPreferences.DarkTheme = IsDarkMode;
        await _userPreferencesService.SaveUserPreferences(_userPreferences);
        OnMajorUpdateOccured();
    }
}
