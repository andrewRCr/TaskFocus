using System.Threading;
using System.Threading.Tasks;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Data.Services
{
    public partial class DataService
    {
        private int _settingsUpdateEntered = 0;

        // helper methods
        // ====================

        // data state CRUD operations
        // ====================

        // for front-end access to data state
        //public List<UserSettingsDisplayModel>? GetDataStateUserSettings() => _dataState.GetWorkingUserSettings();

        // for populating local data state
        public async Task FetchRemoteSettingsData()
        {
            var userSettings = await _userEndpoint.GetCurrentUserSettings();
            _dataHelper.UserSettingsLastFetch = userSettings; // store for comparison

            var displayUserSettings = _mapper.Map<UserSettingsDisplayModel>(userSettings);
            _dataState.UserSettings = displayUserSettings;
        }

        // TODO: needs updating, modeled after Task equivalent
        // validates request, performs additional processing, flags for sync, refreshes UI
        public async Task UpdateSettingsData(UserSettingsDisplayModel displaySettings)
        {
            // re-map
            UserSettingsModel settings = _mapper.Map<UserSettingsModel>(displaySettings);

            if (_dataHelper.HasSettingsDataChanged(settings))
            {
                // lock
                if (Interlocked.Increment(ref _settingsUpdateEntered) != 1) { return; }

                await _userEndpoint.UpdateUserSettings(settings);
                await FetchAllRemoteData();

                // unlock
                Interlocked.Exchange(ref _settingsUpdateEntered, 0);
            }
        }
    }
}
