using System;
using System.Threading;
using System.Threading.Tasks;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Data.Services
{
    public partial class DataService
    {
        private int _userUpdateEntered = 0;

        // helper methods
        // ====================

        public async Task<bool> CheckUserExists(UserModel user)
        {
            bool exists = await _userEndpoint.CheckUserExists(user);
            return exists;
        }

        // data state CRUD operations
        // ====================

        // for front-end access to data state
        //public List<UserDisplayModel>? GetDataStateUser() => _dataState.GetWorkingUser();

        // for populating local data state
        public async Task FetchRemoteUserData()
        {
            var userData = await _userEndpoint.GetCurrentUserData();
            // store for comparison?

            var displayUserData = _mapper.Map<UserDisplayModel>(userData);
            _dataState.CurrentUser = displayUserData;
        }

        // TODO: needs updating, modeled after Task equivalent
        // update local data state: user name data (only)
        // validates request, performs additional processing, flags for sync, refreshes UI
        public async Task UpdateUserNameData(UserDisplayModel displayUserModel)
        {
            if (_dataState.IsDataLoaded())
            {
                // re-map
                UserModel user = _mapper.Map<UserModel>(displayUserModel);

                //if... (check if changed from last fetch?)

                // lock
                if (Interlocked.Increment(ref _userUpdateEntered) != 1) { return; }

                // * INSTEAD OF THIS... *
                //await _userEndpoint.UpdateName(user);
                //await FetchAllRemoteData();

                // * ONLY CHANGE LOCALLY AND MARK FOR SYNC *
                // update local datastate
                _dataState.CurrentUser!.FirstName = user.FirstName;
                _dataState.CurrentUser.LastName = user.LastName;

                // flag for sync
                _dataState.CurrentUser.ClientLastUpdated = DateTimeOffset.Now;
                _dataState.ChangedUserData.Add(_mapper.Map<UserModel>(_dataState.CurrentUser));

                // unlock
                Interlocked.Exchange(ref _userUpdateEntered, 0);

                Console.WriteLine("local user name updated!");
            }
        }

        public async Task RequestUpdateEmail(UserModel user)
        {
            //if... (check if changed from last fetch?)

            // lock
            if (Interlocked.Increment(ref _userUpdateEntered) != 1) { return; }

            await _userEndpoint.RequestUpdateEmail(user);
            await FetchAllRemoteData();

            // unlock
            Interlocked.Exchange(ref _userUpdateEntered, 0);
        }

        public async Task UpdatePassword(CreateUserModel updatedUserModel)
        {
            //if... (check if changed from last fetch?)

            // lock
            if (Interlocked.Increment(ref _userUpdateEntered) != 1) { return; }

            await _userEndpoint.UpdatePassword(updatedUserModel);

            // unlock
            Interlocked.Exchange(ref _userUpdateEntered, 0);
        }
    }
}
