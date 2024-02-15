using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskFocusAPI.Library.Internal.DataAccess;
using TaskFocusAPI.Library.Models;

namespace TaskFocusAPI.Library.DataAccess
{
    public class ProjectData
    {
        private readonly IConfiguration _config;

        public ProjectData(IConfiguration config)
        {
            _config = config;
        }

        public List<ProjectModel> GetAllProjectsForUser(string userId)
        {
            SqlDataAccess sql = new SqlDataAccess(_config);
            var p = new { Id = userId };
            var projects = sql.LoadData<ProjectModel, dynamic>("dbo.spProject_GetAllForUser", p, "TaskFocusData");

            return projects;
        }
    }
}
