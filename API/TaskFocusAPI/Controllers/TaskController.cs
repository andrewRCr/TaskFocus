using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskFocusAPI.Library.DataAccess;
using TaskFocusAPI.Library.Models;

namespace TaskFocusAPI.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class TaskController : ControllerBase
    {
        private readonly ITaskData _taskData;

        public TaskController(ITaskData taskData)
        {
            _taskData = taskData;
        }

        [HttpGet("{taskId}")]
        public TaskModel GetTaskById(int taskId)
        {
            return _taskData.GetTaskById(taskId);
        }

        [HttpGet]
        public List<TaskModel> GetAllTasksForUser()
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return _taskData.GetAllTasksForUser(userId);
        }

        [HttpGet]
        public List<TaskModel> GetInboxTasksForUser()
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return _taskData.GetInboxTasksForUser(userId);
        }

        [HttpGet("{projectId}")]
        public List<TaskModel> GetAllProjectTasksById(int projectId)
        {
            return _taskData.GetAllProjectTasksById(projectId);
        }

        [HttpGet("{contextId}")]
        public List<TaskModel> GetAllContextTasksById(int contextId)
        {
            return _taskData.GetAllContextTasksById(contextId);
        }

        [HttpPost]
        public void Post(TaskModel newTask)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _taskData.AddTask(newTask, userId);
        }

        [HttpPut]
        public void Put(TaskModel updatedTask)
        {
            _taskData.UpdateTaskData(updatedTask);
        }

        [HttpDelete]
        public void Delete(TaskModel taskToDelete) 
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _taskData.DeleteTask(taskToDelete);
        }
    }
}
