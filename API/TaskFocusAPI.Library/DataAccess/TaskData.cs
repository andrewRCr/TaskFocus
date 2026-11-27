using System;
using System.Collections.Generic;
using System.Linq;
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

        public TaskModel GetTaskById(int taskId, string userId)
        {
            var p = new { Id = taskId, UserId = userId };
            var task = _sqlDataAccess.LoadData<TaskModel, dynamic>("dbo.spTask_GetByIdForUser", p, "TaskFocusData").FirstOrDefault();

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

        public List<TaskModel> GetAllProjectTasksById(int projectId, string userId)
        {
            var p = new { Id = projectId, UserId = userId };
            var allProjectTasks = _sqlDataAccess.LoadData<TaskModel, dynamic>("dbo.spTask_GetAllForProjectIdForUser", p, "TaskFocusData");

            return allProjectTasks;
        }

        public List<TaskModel> GetAllContextTasksById(int contextId, string userId)
        {
            var p = new { Id = contextId, UserId = userId };
            var allContextTasks = _sqlDataAccess.LoadData<TaskModel, dynamic>("dbo.spTask_GetAllForContextIdForUser", p, "TaskFocusData");

            return allContextTasks;
        }

        public TaskModel AddTask(TaskModel newTask, string userId)
        {
            newTask.UserId = userId;
            // completion dates are UTC, which clients use to count the days a completed task is kept
            if (newTask.Completed) { newTask.DateCompleted = DateTime.UtcNow; }

            var insertedRow = _sqlDataAccess.SaveDataAndLoadInsertedRow("dbo.spTask_InsertForUser", newTask, "TaskFocusData");
            return insertedRow.FirstOrDefault();
        }

        public bool DeleteTask(int? taskId, string userId)
        {
            var p = new { Id = taskId, UserId = userId };
            return _sqlDataAccess.LoadData<int, dynamic>("dbo.spTask_DeleteForUser", p, "TaskFocusData").Single() != 0;
        }

        public ResourceUpdateResult UpdateTaskData(TaskModel frontEndTask, string userId)
        {
            if (frontEndTask.Id == null)
            {
                return ResourceUpdateResult.NotFound;
            }

            var dbTask = GetTaskById(frontEndTask.Id.Value, userId);

            if (dbTask == null)
            {
                return ResourceUpdateResult.NotFound;
            }

            // handle DateCompleted/unflagging CleanedUp on removing Completed status
            if (!dbTask.Completed && frontEndTask.Completed) { dbTask.DateCompleted = DateTime.UtcNow; }
            else if (dbTask.Completed && !frontEndTask.Completed) 
            { 
                dbTask.DateCompleted = null;
                frontEndTask.CleanedUp = false;
            }

            // handle general user-editable properties
            dbTask.Completed = frontEndTask.Completed;
            dbTask.TaskName = frontEndTask.TaskName?.Trim();
            dbTask.DueDate = frontEndTask.DueDate;
            dbTask.Starred = frontEndTask.Starred;

            // handle indices
            dbTask.InboxIndex = frontEndTask.InboxIndex;
            dbTask.ProjectIndex = frontEndTask.ProjectIndex;
            dbTask.ContextIndex = frontEndTask.ContextIndex;
            dbTask.TodayIndex = frontEndTask.TodayIndex;

            // these, while not directly editable, will have been updated (if needed) by the front-end prior to call
            // (user can edit ProjectName and ContextName, which front-end assigns Id props based upon)
            dbTask.ProjectId = frontEndTask.ProjectId;
            dbTask.ContextId = frontEndTask.ContextId;
            dbTask.CleanedUp = frontEndTask.CleanedUp;

            // sync meta-data
            dbTask.ClientLastUpdated = frontEndTask.ClientLastUpdated;
            dbTask.ServerLastUpdated = frontEndTask.ServerLastUpdated;

            dbTask.UserId = userId;
            return (ResourceUpdateResult)_sqlDataAccess.LoadData<int, TaskModel>("dbo.spTask_UpdateForUser", dbTask, "TaskFocusData").Single();
        }
    }
}
