using System;
using System.Threading;
using System.Threading.Tasks;
using TaskFocusUI.Library.Data.State;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Data.Services
{
    public partial class DataService
    {
        private int _settingsUpdateEntered = 0;

        // helper methods
        // ====================

        // updates local "working" copy of settings data, for use after sync
        private void UpdateWorkingSettingsFromDataState()
        {
            UserSettingsDisplayModel workingUserSettings = _dataState.GetUserSettings()!.Clone();
            _dataState.SetWorkingUserSettings(workingUserSettings);
        }

        // data state CRUD operations
        // ====================

        // for front-end access to data state
        public UserSettingsDisplayModel? GetDataStateUserSettings() => _dataState.GetWorkingUserSettings();

        // for populating local data state
        public async Task FetchRemoteSettingsData()
        {
            var userSettings = await _userEndpoint.GetCurrentUserSettings();
            var displayUserSettings = _mapper.Map<UserSettingsDisplayModel>(userSettings);

            _dataState.SetUserSettings(displayUserSettings); 
            UpdateWorkingSettingsFromDataState(); // will trigger UI update
        }

        // validates request, performs additional processing, flags for sync, refreshes UI
        public void UpdateSettingsData(UserSettingsDisplayModel workingSettings)
        {
            // the server takes 1 to 90 days; a delay outside that (an emptied desktop field gives 0) keeps its current
            // value, so the other settings still save and cleanup never reads it
            var settings = workingSettings.Clone();
            var current = _dataState.GetUserSettings()!;
            if (settings.CleanUpDelayDays is < 1 or > 90) settings.CleanUpDelayDays = current.CleanUpDelayDays;
            if (settings.DeleteDelayDays is < 1 or > 90) settings.DeleteDelayDays = current.DeleteDelayDays;

            if (_dataHelper.HasSettingsDataChanged(settings))
            {
                // lock
                if (Interlocked.Increment(ref _settingsUpdateEntered) != 1) { return; }

                // update data state Settings object from WorkingSettings copy
                settings.ClientLastUpdated = workingSettings.ClientLastUpdated = DateTimeOffset.Now; // flag for sync
                current.ValueAssign(settings);

                // add copy to ChangedSettingsData
                // don't duplicate if already had another update prior to push; update values instead
                if (_dataState.GetChangedSettingsData() == null) _dataState.SetChangedSettingsData(settings.Clone());
                else _dataState.GetChangedSettingsData()!.ValueAssign(settings);

                    // unlock; UI update will be triggered by PropertyChanged call on property set
                    Interlocked.Exchange(ref _settingsUpdateEntered, 0);
            }
        }
    }
}
