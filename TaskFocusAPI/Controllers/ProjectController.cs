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
    public class ProjectController : ApiController
    {
        //[HttpGet]
        public List<ProjectModel> Get()
        {
            string userId = RequestContext.Principal.Identity.GetUserId();
            ProjectData data = new ProjectData();

            return data.GetAllUserProjects(userId);
        }
    }
}
