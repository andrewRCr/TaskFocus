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
        public List<TaskModel> GetAll()
        {
            string userId = RequestContext.Principal.Identity.GetUserId();
            TaskData data = new TaskData();

            return data.GetAllUserTasks(userId);
        }

        public List<TaskModel> GetInbox()
        {
            string userId = RequestContext.Principal.Identity.GetUserId();
            TaskData data = new TaskData();

            return data.GetInboxUserTasks(userId);
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
