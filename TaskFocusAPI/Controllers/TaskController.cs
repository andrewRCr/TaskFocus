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
        //[HttpGet]
        public List<TaskModel> Get()
        {
            string userId = RequestContext.Principal.Identity.GetUserId();
            TaskData data = new TaskData();

            return data.GetAllUserTasks(userId);
        }
    }
}
