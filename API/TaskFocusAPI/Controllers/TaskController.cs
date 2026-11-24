using Microsoft.AspNetCore.Authorization;
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
        public ActionResult<TaskModel> GetTaskById(int taskId)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var task = _taskData.GetTaskById(taskId, userId);
            return task == null ? NoContent() : task;
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
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return _taskData.GetAllProjectTasksById(projectId, userId);
        }

        [HttpGet("{contextId}")]
        public List<TaskModel> GetAllContextTasksById(int contextId)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return _taskData.GetAllContextTasksById(contextId, userId);
        }

        [HttpPost]
        public ActionResult<TaskModel> Post(TaskModel newTask)
        {
            if (newTask.TaskName == null) { return BadRequest(); }

            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var insertedTask = _taskData.AddTask(newTask, userId);
            return insertedTask == null ? BadRequest() : insertedTask;
        }

        [HttpPut]
        public IActionResult Put(TaskModel updatedTask)
        {
            if (updatedTask.Id is null or <= 0 || updatedTask.TaskName == null) { return BadRequest(); }

            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return _taskData.UpdateTaskData(updatedTask, userId) switch
            {
                ResourceUpdateResult.Updated => Ok(),
                ResourceUpdateResult.InvalidParent => BadRequest(),
                _ => NotFound()
            };
        }

        [HttpDelete]
        public IActionResult Delete(TaskModel taskToDelete)
        {
            if (taskToDelete.Id is null or <= 0) { return BadRequest(); }

            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _taskData.DeleteTask(taskToDelete.Id, userId);
            return Ok();
        }
    }
}
