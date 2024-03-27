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
        public void UpdateProjectData(ProjectModel frontEndProject)
        {
            if (frontEndProject.Id == null)
            {
                throw new Exception($"The provided project's Id was a null value.");
            }

            var dbProject = GetProjectById((int)frontEndProject.Id);

            if (dbProject == null)
            {
                throw new Exception($"The project Id of {frontEndProject.Id} could not be found in the database.");
            }

            if (!dbProject.Completed && frontEndProject.Completed) { dbProject.DateCompleted = DateTime.Now; }
            else if (dbProject.Completed && !frontEndProject.Completed) { dbProject.DateCompleted = null; }

            dbProject.Completed = frontEndProject.Completed;
            dbProject.ProjectName = frontEndProject.ProjectName.Trim();
            dbProject.DueDate = frontEndProject.DueDate;
            dbProject.OrderIndex = frontEndProject.OrderIndex;

            // these will have been updated by the front-end prior to call
            dbProject.ContextId = frontEndProject.ContextId;

            try
            {
                _sqlDataAccess.SaveData("dbo.spProject_Update", dbProject, "TaskFocusData");
            }
            catch (System.Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public void DeleteProject(ProjectModel projectToDelete)
        {
            var p = new { Id = projectToDelete.Id };
            _sqlDataAccess.SaveData("dbo.spProject_Delete", p, "TaskFocusData");
        }

    }
}
