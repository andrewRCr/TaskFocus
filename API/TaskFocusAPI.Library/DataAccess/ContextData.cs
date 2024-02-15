using System;
using System.Collections.Generic;
using System.Text;
using TaskFocusAPI.Library.Internal.DataAccess;
using TaskFocusAPI.Library.Models;

namespace TaskFocusAPI.Library.DataAccess
{
    public class ContextData : IContextData
    {
        private readonly ISqlDataAccess _sqlDataAccess;

        public ContextData(ISqlDataAccess sqlDataAccess)
        {
            _sqlDataAccess = sqlDataAccess;
        }

        public List<ContextModel> GetAllContextsForUser(string userId)
        {
            var p = new { Id = userId };
            var contexts = _sqlDataAccess.LoadData<ContextModel, dynamic>("dbo.spContext_GetAllForUser", p, "TaskFocusData");

            return contexts;
        }
    }
}
