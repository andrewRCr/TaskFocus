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
        private readonly IConfiguration _config;

        public TaskController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet]
        public List<TaskModel> GetAllTasksForUser()
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            TaskData data = new TaskData(_config);

            return data.GetAllTasksForUser(userId);
        }

        [HttpGet]
        public List<TaskModel> GetInboxTasksForUser()
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            TaskData data = new TaskData(_config);

            return data.GetInboxTasksForUser(userId);
        }

        [HttpPost]
        public void Post(TaskModel newTask)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            TaskData data = new TaskData(_config);

            data.AddTask(newTask, userId);
        }

        [HttpPut]
        public void Put(TaskModel updatedTask)
        {
            TaskData data = new TaskData(_config);

            data.UpdateTaskData(updatedTask);
        }
    }
}
