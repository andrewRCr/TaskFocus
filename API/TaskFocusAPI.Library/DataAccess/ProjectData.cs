using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public ProjectModel GetProjectById(int projectId)
        {
            var p = new { Id = projectId };
            var project = _sqlDataAccess.LoadData<ProjectModel, dynamic>("dbo.spProject_GetById", p, "TaskFocusData").FirstOrDefault();

            return project;
        }

        public List<ProjectModel> GetAllProjectsForUser(string userId)
        {
            var p = new { Id = userId };
            var projects = _sqlDataAccess.LoadData<ProjectModel, dynamic>("dbo.spProject_GetAllForUser", p, "TaskFocusData");

            return projects;
        }

        public void AddProject(ProjectModel newProject, string userId)
        {
            newProject.UserId = userId;
            if (newProject.Completed) { newProject.DateCompleted = DateTime.Now; }

            _sqlDataAccess.SaveData("dbo.spProject_Insert", newProject, "TaskFocusData");
        }

        public void DeleteProject(ProjectModel projectToDelete)
        {
            throw new NotImplementedException();
        }

        public void UpdateProjectData(ProjectModel frontEndProject)
        {
            throw new NotImplementedException();
        }
    }
}
