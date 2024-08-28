using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.API
{
    public interface ITaskEndpoint
    {
        Task<List<TaskModel>> GetAllTasksForUser();
        Task<List<TaskModel>> GetInboxTasksForUser();
        Task AddTask(TaskModel task, string userId);
        Task DeleteTask(TaskModel task);
        Task UpdateTask(TaskModel task);
        Task<TaskModel> GetTaskById(int taskId);
        Task<List<TaskModel>> GetAllProjectTasksById(int projectId);
        Task<List<TaskModel>> GetAllContextTasksById(int contextId);
    }
}