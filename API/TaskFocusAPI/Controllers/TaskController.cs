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
    }
}
