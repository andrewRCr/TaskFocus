using System.Collections.Generic;
using TaskFocusAPI.Library.Models;

namespace TaskFocusAPI.Library.DataAccess
{
    public interface ITaskData
    {
        TaskModel GetTaskById(int taskId);
        List<TaskModel> GetAllTasksForUser(string userId);
        List<TaskModel> GetInboxTasksForUser(string userId);
        TaskModel AddTask(TaskModel newTask, string userId);
        void DeleteTask(TaskModel taskToDelete);
        void UpdateTaskData(TaskModel frontEndTask);
        List<TaskModel> GetAllProjectTasksById(int projectId);
        List<TaskModel> GetAllContextTasksById(int contextId);
    }
}