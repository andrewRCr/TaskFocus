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
            var userData = _sqlDataAccess.LoadData<UserModel, dynamic>("dbo.spUserLookup", p, "TaskFocusData");

            return userData;
        }

        public List<UserSettingsModel> GetUserSettingsById(string id) 
        { 
            var p = new { Id = id };
            var userSettingsData = _sqlDataAccess.LoadData<UserSettingsModel, dynamic>("dbo.spUserSettings_GetById", p, "TaskFocusData");

            return userSettingsData;
        }

        public void CreateUser(UserModel user)
        {
            var p = new { Id = user.Id, user.FirstName, user.LastName, user.EmailAddress };
            _sqlDataAccess.SaveData("dbo.spUser_Insert", p, "TaskFocusData");

            // make default settings entry
            var v = new { Id = user.Id };
            _sqlDataAccess.SaveData("dbo.spUserSettings_Insert", v, "TaskFocusData");
        }
    }
}
