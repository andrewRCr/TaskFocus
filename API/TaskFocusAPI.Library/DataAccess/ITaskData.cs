using System.Collections.Generic;
using TaskFocusAPI.Library.Models;

namespace TaskFocusAPI.Library.DataAccess
{
    public interface ITaskData
    {
        TaskModel GetTaskById(int taskId, string userId);
        List<TaskModel> GetAllTasksForUser(string userId);
        List<TaskModel> GetInboxTasksForUser(string userId);
        TaskModel AddTask(TaskModel newTask, string userId);
        bool DeleteTask(int? taskId, string userId);
        ResourceUpdateResult UpdateTaskData(TaskModel frontEndTask, string userId);
        List<TaskModel> GetAllProjectTasksById(int projectId, string userId);
        List<TaskModel> GetAllContextTasksById(int contextId, string userId);
    }
}