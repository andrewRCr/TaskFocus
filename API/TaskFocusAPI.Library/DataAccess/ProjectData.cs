using System;
using System.Collections.Generic;
using System.Linq;
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

        public ProjectModel GetProjectById(int projectId, string userId)
        {
            var p = new { Id = projectId, UserId = userId };
            var project = _sqlDataAccess.LoadData<ProjectModel, dynamic>("dbo.spProject_GetByIdForUser", p, "TaskFocusData").FirstOrDefault();

            return project;
        }

        public List<ProjectModel> GetAllProjectsForUser(string userId)
        {
            var p = new { Id = userId };
            var projects = _sqlDataAccess.LoadData<ProjectModel, dynamic>("dbo.spProject_GetAllForUser", p, "TaskFocusData");

            return projects;
        }

        public ProjectModel AddProject(ProjectModel newProject, string userId)
        {
            newProject.UserId = userId;
            if (newProject.Completed) { newProject.DateCompleted = DateTime.Now; }

            var insertedRow = _sqlDataAccess.SaveDataAndLoadInsertedRow("dbo.spProject_InsertForUser", newProject, "TaskFocusData");
            return insertedRow.FirstOrDefault();
        }
        public ResourceUpdateResult UpdateProjectData(ProjectModel frontEndProject, string userId)
        {
            if (frontEndProject.Id == null)
            {
                return ResourceUpdateResult.NotFound;
            }

            var dbProject = GetProjectById(frontEndProject.Id.Value, userId);

            if (dbProject == null)
            {
                return ResourceUpdateResult.NotFound;
            }

            if (!dbProject.Completed && frontEndProject.Completed) { dbProject.DateCompleted = DateTime.Now; }
            else if (dbProject.Completed && !frontEndProject.Completed) { dbProject.DateCompleted = null; }

            dbProject.Completed = frontEndProject.Completed;
            dbProject.ProjectName = frontEndProject.ProjectName?.Trim();
            dbProject.DueDate = frontEndProject.DueDate;
            dbProject.OrderIndex = frontEndProject.OrderIndex;

            // these will have been updated by the front-end prior to call
            dbProject.ContextId = frontEndProject.ContextId;

            // sync meta-data
            dbProject.ClientLastUpdated = frontEndProject.ClientLastUpdated;
            dbProject.ServerLastUpdated = frontEndProject.ServerLastUpdated;

            dbProject.UserId = userId;
            return (ResourceUpdateResult)_sqlDataAccess.LoadData<int, ProjectModel>("dbo.spProject_UpdateForUser", dbProject, "TaskFocusData").Single();
        }

        public bool DeleteProject(int? projectId, string userId)
        {
            var p = new { Id = projectId, UserId = userId };
            return _sqlDataAccess.LoadData<int, dynamic>("dbo.spProject_DeleteForUser", p, "TaskFocusData").Single() != 0;
        }
    }
}
