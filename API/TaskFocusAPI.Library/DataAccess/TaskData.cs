using Microsoft.Extensions.Configuration;
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
        private readonly IConfiguration _config;

        public TaskData(IConfiguration config)
        {
            _config = config;
        }

        public List<TaskModel> GetAllTasksForUser(string userId)
        {
            SqlDataAccess sql = new SqlDataAccess(_config);
            var p = new { Id = userId };
            var userTasks = sql.LoadData<TaskModel, dynamic>("dbo.spTask_GetAllForUser", p, "TaskFocusData");

            return userTasks;
        }

        public List<TaskModel> GetInboxTasksForUser(string userId)
        {
            SqlDataAccess sql = new SqlDataAccess(_config);
            var p = new { Id = userId };
            var userInboxTasks = sql.LoadData<TaskModel, dynamic>("dbo.spTask_GetInboxForUser", p, "TaskFocusData");

            return userInboxTasks;
        }

        public TaskModel GetTaskById(int taskId)
        {
            SqlDataAccess sql = new SqlDataAccess(_config);
            var p = new { Id = taskId };
            var task = sql.LoadData<TaskModel, dynamic>("dbo.spTask_GetById", p, "TaskFocusData").FirstOrDefault();

            return task;
        }

        public void AddTask(TaskModel newTask, string userId)
        {
            newTask.UserId = userId;
            if (newTask.Completed) { newTask.DateCompleted = DateTime.Now; }

            SqlDataAccess sql = new SqlDataAccess(_config);
            sql.SaveData("dbo.spTask_Insert", newTask, "TaskFocusData");
        }

        public void UpdateTaskData(TaskModel frontEndTask)
        {
            if (frontEndTask.Id == null)
            {
                throw new Exception($"The provided task's Id was a null value.");
            }

            var dbTask = GetTaskById((int)frontEndTask.Id);

            if (dbTask == null)
            {
                throw new Exception($"The task Id of {frontEndTask.Id} could not be found in the database.");
            }

            if (!dbTask.Completed && frontEndTask.Completed) { dbTask.DateCompleted = DateTime.Now; }
            else if (dbTask.Completed && !frontEndTask.Completed) { dbTask.DateCompleted = null; }

            dbTask.Completed = frontEndTask.Completed;
            dbTask.TaskName = frontEndTask.TaskName.Trim();
            dbTask.DueDate = frontEndTask.DueDate;

            // these will have been updated by the front-end prior to call
            dbTask.ProjectId = frontEndTask.ProjectId;
            dbTask.ContextId = frontEndTask.ContextId;

            try
            {
                SqlDataAccess sql = new SqlDataAccess(_config);
                sql.SaveData("dbo.spTask_Update", dbTask, "TaskFocusData");
            }
            catch (System.Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
