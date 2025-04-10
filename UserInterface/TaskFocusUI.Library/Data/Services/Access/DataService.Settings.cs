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
            if (_dataHelper.HasSettingsDataChanged(workingSettings))
            {
                // lock
                if (Interlocked.Increment(ref _settingsUpdateEntered) != 1) { return; }

                // update data state Settings object from WorkingSettings copy
                workingSettings.ClientLastUpdated = DateTimeOffset.Now; // flag for sync
                _dataState.GetUserSettings()!.ValueAssign(workingSettings);

                // add copy to ChangedSettingsData
                // don't duplicate if already had another update prior to push
                if (_dataState.GetChangedSettingsData() == null) _dataState.SetChangedSettingsData(workingSettings.Clone());

                // unlock; UI update will be triggered by PropertyChanged call on property set
                Interlocked.Exchange(ref _settingsUpdateEntered, 0);
            }
        }
    }
}
