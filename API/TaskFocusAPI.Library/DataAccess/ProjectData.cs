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
    public class ProjectData : IProjectData
    {
        private readonly ISqlDataAccess _sqlDataAccess;

        public ProjectData(ISqlDataAccess sqlDataAccess)
        {
            _sqlDataAccess = sqlDataAccess;
        }

        public List<ProjectModel> GetAllProjectsForUser(string userId)
        {
            var p = new { Id = userId };
            var projects = _sqlDataAccess.LoadData<ProjectModel, dynamic>("dbo.spProject_GetAllForUser", p, "TaskFocusData");

            return projects;
        }
    }
}
