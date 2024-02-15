using System.Collections.Generic;
using TaskFocusAPI.Library.Models;

namespace TaskFocusAPI.Library.DataAccess
{
    public interface ITaskData
    {
        void AddTask(TaskModel newTask, string userId);
        List<TaskModel> GetAllTasksForUser(string userId);
        List<TaskModel> GetInboxTasksForUser(string userId);
        TaskModel GetTaskById(int taskId);
        void UpdateTaskData(TaskModel frontEndTask);
    }
}