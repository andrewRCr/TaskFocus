using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskFocusAPI.Library.Internal.DataAccess;
using TaskFocusAPI.Library.Models;

namespace TaskFocusAPI.Library.DataAccess
{
    public class TaskData
    {
        public List<TaskModel> GetAllUserTasks(string id)
        {
            SqlDataAccess sql = new SqlDataAccess();
            var p = new { Id = id };
            var userTasks = sql.LoadData<TaskModel, dynamic>("dbo.spTask_GetAllForUser", p, "TaskFocusData");

            return userTasks;
        }
    }
}
