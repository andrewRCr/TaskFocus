using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using TaskFocusAPI.Library.DataAccess;
using TaskFocusAPI.Library.Models;

namespace TaskFocusAPI.Controllers
{
    [Authorize]
    public class TaskController : ApiController
    {
        public List<TaskModel> GetAllTasksForUser()
        {
            string userId = RequestContext.Principal.Identity.GetUserId();
            TaskData data = new TaskData();

            return data.GetAllTasksForUser(userId);
        }

        public List<TaskModel> GetInboxTasksForUser()
        {
            string userId = RequestContext.Principal.Identity.GetUserId();
            TaskData data = new TaskData();

            return data.GetInboxTasksForUser(userId);
        }

        public void Post(TaskModel newTask)
        {
            string userId = RequestContext.Principal.Identity.GetUserId();
            TaskData data = new TaskData();

            data.AddTask(newTask, userId);
        }

        public void Put(TaskModel updatedTask)
        {
            TaskData data = new TaskData();

            data.UpdateTaskData(updatedTask);
        }
    }
}
