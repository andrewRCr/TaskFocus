using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskFocusAPI.Library.Models;

namespace TaskFocusAPI.Library.DataAccess
{
    public class TaskData : ITaskData
    {
        private readonly ISqlDataAccess _sqlDataAccess;

        public TaskData(ISqlDataAccess sqlDataAccess)
        {
            _sqlDataAccess = sqlDataAccess;
        }

        public TaskModel GetTaskById(int taskId)
        {
            var p = new { Id = taskId };
            var task = _sqlDataAccess.LoadData<TaskModel, dynamic>("dbo.spTask_GetById", p, "TaskFocusData").FirstOrDefault();

            return task;
        }

        public List<TaskModel> GetAllTasksForUser(string userId)
        {
            var p = new { Id = userId };
            var userTasks = _sqlDataAccess.LoadData<TaskModel, dynamic>("dbo.spTask_GetAllForUser", p, "TaskFocusData");

            return userTasks;
        }

        public List<TaskModel> GetInboxTasksForUser(string userId)
        {
            var p = new { Id = userId };
            var userInboxTasks = _sqlDataAccess.LoadData<TaskModel, dynamic>("dbo.spTask_GetInboxForUser", p, "TaskFocusData");

            return userInboxTasks;
        }

        public void AddTask(TaskModel newTask, string userId)
        {
            newTask.UserId = userId;
            if (newTask.Completed) { newTask.DateCompleted = DateTime.Now; }

            _sqlDataAccess.SaveData("dbo.spTask_Insert", newTask, "TaskFocusData");
        }

        public void DeleteTask(TaskModel taskToDelete)
        {
            var p = new { Id = taskToDelete.Id };
            _sqlDataAccess.SaveData("dbo.spTask_Delete", p, "TaskFocusData");
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

            dbTask.InboxIndex = frontEndTask.InboxIndex;
            dbTask.ProjectIndex = frontEndTask.ProjectIndex;
            dbTask.ContextIndex = frontEndTask.ContextIndex;

            // these will have been updated by the front-end prior to call
            dbTask.ProjectId = frontEndTask.ProjectId;
            dbTask.ContextId = frontEndTask.ContextId;

            try
            {
                _sqlDataAccess.SaveData("dbo.spTask_Update", dbTask, "TaskFocusData");
            }
            catch (System.Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
