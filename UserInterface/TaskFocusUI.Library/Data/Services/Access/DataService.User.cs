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

        // wrappers, so front-end only deals with the data service
        public async Task<bool> CheckUserExists(UserModel user) => await _userEndpoint.CheckUserExists(user);
        public async Task<bool> CheckPasswordValid(CheckPasswordModel checkPasswordModel) => await _userEndpoint.CheckPasswordValid(checkPasswordModel);
        public async Task<bool> CheckUserEmailConfirmed(UserModel userModel) => await _userEndpoint.CheckUserEmailConfirmed(userModel);
        public async Task ConfirmEmail(ConfirmEmailModel confirmEmailModel) => await _userEndpoint.ConfirmEmail(confirmEmailModel);
        public async Task ConfirmUpdatedEmail(ConfirmUpdatedEmailModel confirmUpdatedEmailModel) => await _userEndpoint.ConfirmUpdatedEmail(confirmUpdatedEmailModel);
        public async Task SendEmailConfirmationLink(UserModel userModel) => await _userEndpoint.SendEmailConfirmationLink(userModel);
        public async Task SendPasswordResetEmail(UserModel userModel) => await _userEndpoint.SendPasswordResetEmail(userModel);
        public async Task SendPasswordChangeSuccessEmail(UserModel user) => await _userEndpoint.SendPasswordChangeSuccessEmail(user);

        // data state CRUD operations
        // ====================

        // for front-end access to data state
        public UserDisplayModel? GetDataStateCurrentUser() => _dataState.GetWorkingCurrentUser();

        // for bypassing display mapping
        public async Task<UserModel> GetRawCurrentUserData() => await _userEndpoint.GetCurrentUserData();

        // for populating local data state
        public async Task FetchRemoteUserData()
        {
            var userData = await _userEndpoint.GetCurrentUserData();
            var displayUserData = _mapper.Map<UserDisplayModel>(userData);
            _dataState.SetCurrentUser(displayUserData); 
            UpdateWorkingCurrentUserFromDataState(); // will trigger UI update
        }

        public async Task CreateUser(CreateUserModel userModel) => await _userEndpoint.CreateUser(userModel);

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
                    if (_dataState.GetChangedUserData() == null) _dataState.SetChangedUserData(workingCurrentUser.Clone());

                    // unlock; UI update will be triggered by PropertyChanged call on property set
                    Interlocked.Exchange(ref _userUpdateEntered, 0);
                }
            }
        }

        public async Task<bool> RequestUpdateEmail(UserModel user)
        {
            // lock
            if (Interlocked.Increment(ref _userUpdateEntered) != 1) { return false; }

            bool result = await _userEndpoint.RequestUpdateEmail(user);
            // ensure local data state updated, as this property is user-editable and visible
            InvokeSyncRequest(nameof(RequestUpdateEmail)); 

            // unlock
            Interlocked.Exchange(ref _userUpdateEntered, 0);
            return result;
        }

        public async Task UpdatePassword(CreateUserModel updatedUserModel)
        {
            // lock
            if (Interlocked.Increment(ref _userUpdateEntered) != 1) { return; }

            await _userEndpoint.UpdatePassword(updatedUserModel);

            // unlock
            Interlocked.Exchange(ref _userUpdateEntered, 0);
        }

        public async Task ResetPassword(ResetPasswordModel resetPasswordModel) => await _userEndpoint.ResetPassword(resetPasswordModel);




    }
}
