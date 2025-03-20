using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaskFocusUI.Library.Data.Services.Access;
using TaskFocusUI.Library.Data.State;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Data.Services
{
    public partial class DataService
    {
        private int _projectUpdateEntered = 0;
        private ProjectDisplayModel? _projectBeingUpdated;

        // helper methods
        // ====================

        // do I need this? not currently being called... TODO: re-examine why it's in use for Tasks; is it only needed there and not for Projects/Contexts?
        public bool IsProjectCurrentlyBeingUpdated(ProjectDisplayModel project)
        {
            if (_projectBeingUpdated != null)
            {
                if (project.Id == null && project.TempLocalId != null)
                {
                    return _projectBeingUpdated.TempLocalId == project.TempLocalId;
                }
                else { return _projectBeingUpdated.Id == project.Id; }
            }

            return false;
        }

        // data state CRUD operations
        // ====================

        // for front-end access to data state
        public List<ProjectDisplayModel>? GetDataStateProjects() => _dataState.GetWorkingProjects();

        // for populating local data state
        public async Task FetchRemoteProjectData()
        {
            var projectList = await _projectEndpoint.GetAllProjectsForUser();
            projectList.Sort((a, b) => Nullable.Compare(a.OrderIndex, b.OrderIndex));

            var displayProjectList = _mapper.Map<List<ProjectDisplayModel>>(projectList);
            _dataState.SetProjects(displayProjectList);

            List<ProjectDisplayModel> workingDisplayProjectList = displayProjectList.ConvertAll(project => project.Clone());
            _dataState.SetWorkingProjects(workingDisplayProjectList);

            _dataState.InvokeDataStateChanged("Projects");
        }

        public async Task FetchRemoteProjectAndTasksById(int id)
        {
            var project = await _projectEndpoint.GetProjectById(id);
            var displayProject = _mapper.Map<ProjectDisplayModel>(project);
            _dataHelper.FocusedProject = displayProject;

            var projectTasks = await _taskEndpoint.GetAllProjectTasksById(id);
            var displayProjectTasks = _mapper.Map<List<TaskDisplayModel>>(projectTasks);
            _dataHelper.FocusedProjectTasks = displayProjectTasks;
        }

        // TODO: needs testing after new changes
        // validates request, processes local add, flags for sync, refreshes UI
        public ProjectDisplayModel? AddProject(ProjectModel newProject)
        {
            if (string.IsNullOrWhiteSpace(newProject.ProjectName)) { return null; }

            if (!_dataHelper.IsNewProjectNameUnique(newProject.ProjectName))
            {
                LogError("Unable to create project: project names must be unique.");
                return null;
            }

            // map to display model
            ProjectDisplayModel newDisplayProject = _mapper.Map<ProjectDisplayModel>(newProject);
            // determine OrderIndex for project
            newProject.OrderIndex = _dataState.GetProjects()!.Count;
            // give temp local tracking id
            newDisplayProject.TempLocalId = ++_dataState.TempProjectId;

            // flag for sync
            newDisplayProject.ClientLastUpdated = DateTimeOffset.Now;
            _dataState.ChangedProjectData.Add(newDisplayProject.Clone());

            // update local data state
            _dataState.GetWorkingProjects()!.Add(newDisplayProject.Clone());
            _dataState.GetProjects()!.Add(newDisplayProject.Clone());

            // trigger UI update
            _dataState.InvokeDataStateChanged("Projects");
            return newDisplayProject;
        }

        // TODO: needs updating, modeled after Task equivalent
        public async Task DeleteProject(ProjectDisplayModel displayProject)
        {
            // map from ProjectDisplayModel to ProjectModel
            ProjectModel project = _mapper.Map<ProjectModel>(displayProject);

            // ensure other projects have updated OrderIndex values
            this.ShiftCollectionOrderIndices(displayProject, _dataState.GetProjects()!);

            // handle project's tasks - remove assigned project
            List<TaskDisplayModel> projectTasks = _dataState.GetTasks()!.Where(x => x.ProjectId == project.Id).ToList();
            foreach (TaskDisplayModel task in projectTasks)
            {
                task.ProjectName = null;
                await UpdateTaskData(task);
            }

            // update local data state
            if (project.Id == null) // never existed on server; insert was pending push
            {
                // local delete
                _dataState.GetProjects()!.Remove(displayProject);
                var changedProject = _dataState.ChangedProjectData.Find(x => x.TempLocalId == displayProject.TempLocalId);
                bool removed = _dataState.ChangedProjectData.Remove(changedProject!);
                // trigger UI update
                _dataState.InvokeDataStateChanged("Projects");

                if (removed)
                    Console.WriteLine("never existed on server; removed from changed project data");
                else
                    Console.WriteLine("never existed on server; COULDN'T FIND IN CHANGEDPROJECTDATA TO REMOVE!");
            }
            else
            {
                ProjectDisplayModel clientProject = _dataState.GetProjects()!.Find(x => x.Id == project.Id)!;
                clientProject.Deleted = DateTimeOffset.Now;
                // flag for sync
                clientProject.ClientLastUpdated = DateTimeOffset.Now;

                // if had an update pending push, don't add a duplicate to changedProjectData
                var alreadyQueued = _dataState.ChangedProjectData.Where(
                    x => x.Id == clientProject.Id);
                if (alreadyQueued.Count() == 0) { _dataState.ChangedProjectData.Add(clientProject.Clone()); }

                // local delete
                _dataState.GetProjects()!.Remove(clientProject);
                // trigger UI update
                _dataState.InvokeDataStateChanged("Projects");
            }
        }

        // TODO: needs updating, modeled after Task equivalent
        // validates request, performs additional processing, flags for sync, refreshes UI
        public async Task UpdateProjectData(ProjectDisplayModel workingProject)
        {
            // map from ProjectDisplayModel to ProjectModel
            ProjectModel project = _mapper.Map<ProjectModel>(workingProject);

            if (_dataHelper.HasProjectDataChanged(workingProject))
            {
                if (!_dataHelper.IsUpdatedProjectNameUnique(workingProject))
                {
                    LogError("Unable to update project: project names must be unique.");
                    return;
                }

                if (_projectBeingUpdated != null)
                {
                    if (project.Id == null && workingProject.TempLocalId != _projectBeingUpdated.TempLocalId ||
                        project.Id != _projectBeingUpdated.Id)
                    {
                        // unlock
                        Interlocked.Exchange(ref _projectUpdateEntered, 0);
                        _projectBeingUpdated = null;
                    }
                }

                // lock
                if (Interlocked.Increment(ref _projectUpdateEntered) != 1) { return; }
                _projectBeingUpdated = workingProject;

                // change locally and mark for sync
                if (workingProject.Id == null) // project hasn't yet been inserted on server; pending push
                {
                    // update standard client data state copy
                    var dataStateProject = _dataState.GetProjects()!.Find(x => x.TempLocalId == workingProject.TempLocalId);
                    if (dataStateProject != null) dataStateProject.ValueAssign(workingProject);

                    // update changedProjectData copy of task
                    // note: only need to track pending property changes in changedProjectData if project has never been pushed
                    var queuedChangedProject = _dataState.ChangedProjectData!.Find(x => x.TempLocalId == workingProject.TempLocalId);
                    if (queuedChangedProject != null) queuedChangedProject.ValueAssign(workingProject);
                }
                else
                {
                    // update standard client data state copy
                    var dataStateProject = _dataState.GetProjects()!.Find(x => x.Id == workingProject.Id);
                    if (dataStateProject != null) dataStateProject.ValueAssign(workingProject);

                    // add copy to ChangedProjectData
                    // don't duplicate if already had another update prior to push
                    var alreadyQueued = _dataState.ChangedProjectData.Where(
                        x => x.Id == workingProject.Id);
                    if (!alreadyQueued.Any()) { _dataState.ChangedProjectData.Add(workingProject.Clone()); }
                }

                workingProject.ClientLastUpdated = DateTimeOffset.Now; // flag for sync
                // unlock
                Interlocked.Exchange(ref _projectUpdateEntered, 0);
                _projectBeingUpdated = null;
                // trigger UI update
                _dataState.InvokeDataStateChanged("Projects");
            }
        }
    }
}
