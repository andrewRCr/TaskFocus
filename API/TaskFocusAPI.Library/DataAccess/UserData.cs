using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public void UpdateSettingsData(UserSettingsModel frontEndSettings)
        {
            if (frontEndSettings.Id == null)
            {
                throw new Exception($"The provided UserSettings's Id was a null value.");
            }

            var dbSettings = GetUserSettingsById(frontEndSettings.Id);

            if (dbSettings == null)
            {
                throw new Exception($"The Settings Id of {frontEndSettings.Id} could not be found in the database.");
            }

            dbSettings.CleanUpImmediately = frontEndSettings.CleanUpImmediately;
            dbSettings.CleanUpDelayDays = frontEndSettings.CleanUpDelayDays;

            try
            {
                _sqlDataAccess.SaveData("dbo.spUserSettings_Update", dbSettings, "TaskFocusData");
            }
            catch (System.Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public void CreateUser(UserModel user)
        {
            var p = new { Id = user.Id, user.FirstName, user.LastName, user.Email };
            _sqlDataAccess.SaveData("dbo.spUser_Insert", p, "TaskFocusData");

            // make default settings entry
            var v = new { Id = user.Id };
            _sqlDataAccess.SaveData("dbo.spUserSettings_Insert", v, "TaskFocusData");
        }
    }
}
