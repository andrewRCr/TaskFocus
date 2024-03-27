using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public ContextModel GetContextById(int contextId)
        {
            var p = new { Id = contextId };
            var context = _sqlDataAccess.LoadData<ContextModel, dynamic>("dbo.spContext_GetById", p, "TaskFocusData").FirstOrDefault();

            return context;
        }

        public List<ContextModel> GetAllContextsForUser(string userId)
        {
            var p = new { Id = userId };
            var contexts = _sqlDataAccess.LoadData<ContextModel, dynamic>("dbo.spContext_GetAllForUser", p, "TaskFocusData");

            return contexts;
        }

        public void AddContext(ContextModel newContext, string userId)
        {
            newContext.UserId = userId;

            _sqlDataAccess.SaveData("dbo.spContext_Insert", newContext, "TaskFocusData");
        }

        public void UpdateContextData(ContextModel frontEndContext)
        {
            if (frontEndContext.Id == null)
            {
                throw new Exception($"The provided project's Id was a null value.");
            }

            var dbContext = GetContextById((int)frontEndContext.Id);

            if (dbContext == null)
            {
                throw new Exception($"The context Id of {frontEndContext.Id} could not be found in the database.");
            }

            dbContext.ContextName = frontEndContext.ContextName.Trim();
            dbContext.OrderIndex = frontEndContext.OrderIndex;

            try
            {
                _sqlDataAccess.SaveData("dbo.spContext_Update", dbContext, "TaskFocusData");
            }
            catch (System.Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public void DeleteContext(ContextModel contextToDelete)
        {
            var p = new { Id = contextToDelete.Id };
            _sqlDataAccess.SaveData("dbo.spContext_Delete", p, "TaskFocusData");
        }
    }
}
