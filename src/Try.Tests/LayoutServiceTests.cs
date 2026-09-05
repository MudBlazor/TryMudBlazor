namespace Tests
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using TryMudBlazor.Client.Services;
    using TryMudBlazor.Client.Services.UserPreferences;

    /// <summary>
    /// The theme toggle can be pressed while the stored preferences are still loading. These tests pause the
    /// load with a deferred fake service, toggle, then let the load finish and check the user's choice survived.
    /// </summary>
    public class LayoutServiceTests
    {
        [Test]
        public async Task ToggleDuringLoadWinsOverAStoredPreference()
        {
            var preferences = new DeferredPreferencesService();
            var layout = new LayoutService(preferences);

            var loading = layout.ApplyUserPreferences(isDarkModeDefaultTheme: false);
            await layout.ToggleDarkMode();
            preferences.CompleteLoad(new UserPreferences { DarkTheme = false });
            await loading;

            Assert.That(layout.IsDarkMode, Is.True);
            Assert.That(preferences.Saved, Is.EqualTo(new[] { true }));
        }

        [Test]
        public async Task ToggleDuringLoadWinsOverAnEmptyStore()
        {
            var preferences = new DeferredPreferencesService();
            var layout = new LayoutService(preferences);

            var loading = layout.ApplyUserPreferences(isDarkModeDefaultTheme: false);
            await layout.ToggleDarkMode();
            preferences.CompleteLoad(null);
            await loading;

            Assert.That(layout.IsDarkMode, Is.True);
            // The toggle's save is the only one; the load must not write the default on top of it.
            Assert.That(preferences.Saved, Is.EqualTo(new[] { true }));
        }

        [Test]
        public async Task ToggleBeforeAnyLoadIsKeptWhenTheLoadArrivesLater()
        {
            var preferences = new DeferredPreferencesService();
            var layout = new LayoutService(preferences);

            await layout.ToggleDarkMode();
            var loading = layout.ApplyUserPreferences(isDarkModeDefaultTheme: false);
            preferences.CompleteLoad(new UserPreferences { DarkTheme = false });
            await loading;

            Assert.That(layout.IsDarkMode, Is.True);
            Assert.That(preferences.Saved, Is.EqualTo(new[] { true }));
        }

        [Test]
        public async Task StoredPreferenceAppliesWhenNothingWasToggled()
        {
            var preferences = new DeferredPreferencesService();
            var layout = new LayoutService(preferences);

            var loading = layout.ApplyUserPreferences(isDarkModeDefaultTheme: false);
            preferences.CompleteLoad(new UserPreferences { DarkTheme = true });
            await loading;

            Assert.That(layout.IsDarkMode, Is.True);
            Assert.That(preferences.Saved, Is.Empty);
        }

        [Test]
        public async Task EmptyStoreSavesTheDefaultWhenNothingWasToggled()
        {
            var preferences = new DeferredPreferencesService();
            var layout = new LayoutService(preferences);

            var loading = layout.ApplyUserPreferences(isDarkModeDefaultTheme: true);
            preferences.CompleteLoad(null);
            await loading;

            Assert.That(layout.IsDarkMode, Is.True);
            Assert.That(preferences.Saved, Is.EqualTo(new[] { true }));
        }

        [Test]
        public async Task ToggleAfterLoadFlipsTheStoredValue()
        {
            var preferences = new DeferredPreferencesService();
            var layout = new LayoutService(preferences);

            var loading = layout.ApplyUserPreferences(isDarkModeDefaultTheme: false);
            preferences.CompleteLoad(new UserPreferences { DarkTheme = true });
            await loading;
            await layout.ToggleDarkMode();

            Assert.That(layout.IsDarkMode, Is.False);
            Assert.That(preferences.Saved, Is.EqualTo(new[] { false }));
        }

        [Test]
        public async Task SecondLayoutSharesTheFirstLoad()
        {
            var preferences = new DeferredPreferencesService();
            var layout = new LayoutService(preferences);

            var first = layout.ApplyUserPreferences(isDarkModeDefaultTheme: true);
            var second = layout.ApplyUserPreferences(isDarkModeDefaultTheme: false);
            preferences.CompleteLoad(null);
            await Task.WhenAll(first, second);

            Assert.That(preferences.LoadCount, Is.EqualTo(1));
            Assert.That(layout.IsDarkMode, Is.True);
        }

        /// <summary>
        /// A preferences store whose load completes only when the test says so.
        /// </summary>
        private sealed class DeferredPreferencesService : IUserPreferencesService
        {
            private readonly TaskCompletionSource<UserPreferences> _load = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public int LoadCount { get; private set; }

            /// <summary>The DarkTheme value of every save, in order.</summary>
            public List<bool> Saved { get; } = [];

            public void CompleteLoad(UserPreferences stored) => _load.SetResult(stored);

            public Task<UserPreferences> LoadUserPreferences()
            {
                LoadCount++;
                return _load.Task;
            }

            public Task SaveUserPreferences(UserPreferences userPreferences)
            {
                Saved.Add(userPreferences.DarkTheme);
                return Task.CompletedTask;
            }
        }
    }
}
