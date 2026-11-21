using System.Collections.Generic;
using System.Linq;
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

        public ContextModel GetContextById(int contextId, string userId)
        {
            var p = new { Id = contextId, UserId = userId };
            var context = _sqlDataAccess.LoadData<ContextModel, dynamic>("dbo.spContext_GetByIdForUser", p, "TaskFocusData").FirstOrDefault();

            return context;
        }

        public List<ContextModel> GetAllContextsForUser(string userId)
        {
            var p = new { Id = userId };
            var contexts = _sqlDataAccess.LoadData<ContextModel, dynamic>("dbo.spContext_GetAllForUser", p, "TaskFocusData");

            return contexts;
        }

        public ContextModel AddContext(ContextModel newContext, string userId)
        {
            newContext.UserId = userId;

            var insertedRow = _sqlDataAccess.SaveDataAndLoadInsertedRow("dbo.spContext_Insert", newContext, "TaskFocusData");
            return insertedRow.FirstOrDefault();
        }

        public bool UpdateContextData(ContextModel frontEndContext, string userId)
        {
            if (frontEndContext.Id == null)
            {
                return false;
            }

            var dbContext = GetContextById(frontEndContext.Id.Value, userId);

            if (dbContext == null)
            {
                return false;
            }

            dbContext.ContextName = frontEndContext.ContextName.Trim();
            dbContext.OrderIndex = frontEndContext.OrderIndex;

            // sync meta-data
            dbContext.ClientLastUpdated = frontEndContext.ClientLastUpdated;
            dbContext.ServerLastUpdated = frontEndContext.ServerLastUpdated;

            dbContext.UserId = userId;
            return _sqlDataAccess.LoadData<int, ContextModel>("dbo.spContext_UpdateForUser", dbContext, "TaskFocusData").Single() != 0;
        }

        public bool DeleteContext(int? contextId, string userId)
        {
            var p = new { Id = contextId, UserId = userId };
            return _sqlDataAccess.LoadData<int, dynamic>("dbo.spContext_DeleteForUser", p, "TaskFocusData").Single() != 0;
        }
    }
}
