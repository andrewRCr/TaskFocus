using System;
using System.Threading;
using System.Threading.Tasks;
using TaskFocusUI.Library.Data.State;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Data.Services
{
    public partial class DataService
    {
        private int _userUpdateEntered = 0;

        // helper methods
        // ====================

        // updates local "working" copy of current user data, for use after sync
        private void UpdateWorkingCurrentUserFromDataState()
        {
            UserDisplayModel workingCurrentUser = _dataState.GetCurrentUser()!.Clone();
            _dataState.SetWorkingCurrentUser(workingCurrentUser);
        }

        public async Task<bool> CheckUserExists(UserModel user) => await _userEndpoint.CheckUserExists(user);

        public void OnUserLogout()
        {
            _dataState.SetCurrentUser(null);
            UpdateWorkingCurrentUserFromDataState();
        }

        // data state CRUD operations
        // ====================

        // for front-end access to data state
        public UserDisplayModel? GetDataStateCurrentUser() => _dataState.GetWorkingCurrentUser();

        // for populating local data state
        public async Task FetchRemoteUserData()
        {
            var userData = await _userEndpoint.GetCurrentUserData();
            var displayUserData = _mapper.Map<UserDisplayModel>(userData);
            _dataState.SetCurrentUser(displayUserData);
            UpdateWorkingCurrentUserFromDataState();

            _dataState.InvokeDataStateChanged("User"); // TODO: necessary? on set PropertyChanged call should be sufficient if nameof handled right
        }

        // TODO: needs testing after recent updates
        // update local data state: user name data (only)
        // validates request, performs additional processing, flags for sync, refreshes UI
        public void UpdateUserNameData(UserDisplayModel workingCurrentUser)
        {
            if (_dataState.IsDataLoaded())
            {
                if (_dataHelper.HasUserDataChanged(workingCurrentUser))
                {
                    // lock
                    if (Interlocked.Increment(ref _userUpdateEntered) != 1) { return; }

                    // update local data state
                    workingCurrentUser.ClientLastUpdated = DateTimeOffset.Now; // flag for sync
                    _dataState.GetCurrentUser()!.ValueAssign(workingCurrentUser);

                    // add to changedUserData
                    // don't duplicate if already had another update prior to push
                    if (_dataState.ChangedUserData == null) _dataState.ChangedUserData = workingCurrentUser.Clone();

                    // unlock + trigger UI update
                    Interlocked.Exchange(ref _userUpdateEntered, 0);
                    _dataState.InvokeDataStateChanged("User"); // TODO: necessary? on set PropertyChanged call should be sufficient if nameof handled right
                }
            }
        }

        public async Task RequestUpdateEmail(UserModel user)
        {
            // lock
            if (Interlocked.Increment(ref _userUpdateEntered) != 1) { return; }

            await _userEndpoint.RequestUpdateEmail(user);
            // ensure local data state updated, as this property is user-editable and visible
            InvokeSyncRequest(nameof(RequestUpdateEmail)); 

            // unlock
            Interlocked.Exchange(ref _userUpdateEntered, 0);
        }

        public async Task UpdatePassword(CreateUserModel updatedUserModel)
        {
            // lock
            if (Interlocked.Increment(ref _userUpdateEntered) != 1) { return; }

            await _userEndpoint.UpdatePassword(updatedUserModel);

            // unlock
            Interlocked.Exchange(ref _userUpdateEntered, 0);
        }
    }
}
