using System;
using System.Collections.Generic;
using System.Linq;
using TaskFocusAPI.Library.Models;

namespace TaskFocusAPI.Library.DataAccess
{
    public class UserData : IUserData
    {
        private readonly ISqlDataAccess _sqlDataAccess;

        public UserData(ISqlDataAccess sqlDataAccess)
        {
            _sqlDataAccess = sqlDataAccess;
        }

        public List<UserModel> GetUserById(string id)
        {
            var p = new { Id = id };
            var userData = _sqlDataAccess.LoadData<UserModel, dynamic>("dbo.spUser_GetById", p, "TaskFocusData");

            return userData;
        }

        public UserSettingsModel GetUserSettingsById(string id) 
        { 
            var p = new { Id = id };
            var userSettingsData = _sqlDataAccess.LoadData<UserSettingsModel, dynamic>(
                    "dbo.spUserSettings_GetById", p, "TaskFocusData").FirstOrDefault();

            return userSettingsData;
        }

        public bool UpdateSettingsData(UserSettingsModel frontEndSettings, string userId)
        {
            var dbSettings = GetUserSettingsById(userId);

            if (dbSettings == null)
            {
                return false;
            }

            dbSettings.CleanUpImmediately = frontEndSettings.CleanUpImmediately;
            dbSettings.CleanUpDelayDays = frontEndSettings.CleanUpDelayDays;
            dbSettings.DeleteDelayDays = frontEndSettings.DeleteDelayDays;

            // sync meta-data
            dbSettings.ClientLastUpdated = frontEndSettings.ClientLastUpdated;
            dbSettings.ServerLastUpdated = frontEndSettings.ServerLastUpdated;

            try
            {
                _sqlDataAccess.SaveData("dbo.spUserSettings_Update", dbSettings, "TaskFocusData");
            }
            catch (System.Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return true;
        }

        public void CreateUser(UserModel user)
        {
            var p = new { Id = user.Id, user.FirstName, user.LastName, user.Email };
            _sqlDataAccess.SaveData("dbo.spUser_Insert", p, "TaskFocusData");

            // make default settings entry
            var v = new { Id = user.Id };
            _sqlDataAccess.SaveData("dbo.spUserSettings_Insert", v, "TaskFocusData");
        }

        public void UpdateUser(UserModel user)
        {
            try
            {
                _sqlDataAccess.SaveData("dbo.spUser_Update", user, "TaskFocusData");
            }
            catch (System.Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public bool UpdateUserName(UserModel user)
        {
            var p = new { user.Id, user.FirstName, user.LastName, user.ServerLastUpdated, user.ClientLastUpdated };
            return _sqlDataAccess.LoadData<int, dynamic>("dbo.spUser_UpdateName", p, "TaskFocusData").Single() != 0;
        }
    }
}
