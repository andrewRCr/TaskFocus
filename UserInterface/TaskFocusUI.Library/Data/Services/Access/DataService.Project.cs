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

        private async Task ProcessLocalProjectUpdate(ProjectDisplayModel workingProject, bool nameChanged = false)
        {
            if (nameChanged)
            {
                // handle project's tasks - update assigned project name
                List<TaskDisplayModel> projectTasks;
                if (workingProject.Id != null)
                {

                    projectTasks = _dataState.GetWorkingTasks()!.Where(x => x.ProjectId == workingProject.Id).ToList();
                    foreach (TaskDisplayModel task in projectTasks)
                    {
                        task.ProjectName = workingProject.ProjectName;
                        UpdateTaskData(task);
                    }
                }
            }

            // update data state Projects object from WorkingProjects copy
            workingProject.ClientLastUpdated = DateTimeOffset.Now; // flag for sync
            if (workingProject.Id == null) // project hasn't yet been inserted on server; pending push
            {
                // update standard client data state copy
                var dataStateProject = _dataState.GetProjects()!.Find(x => x.TempLocalId == workingProject.TempLocalId);
                if (dataStateProject != null) dataStateProject.ValueAssign(workingProject);

                // update changedProjectData copy of task
                // note: only need to track pending property changes in changedProjectData if project has never been pushed
                var queuedChangedProject = _dataState.GetChangedProjectData().Find(x => x.TempLocalId == workingProject.TempLocalId);
                if (queuedChangedProject != null) queuedChangedProject.ValueAssign(workingProject);
            }
            else
            {
                // update standard client data state copy
                var dataStateProject = _dataState.GetProjects()!.Find(x => x.Id == workingProject.Id);
                if (dataStateProject != null) dataStateProject.ValueAssign(workingProject);

                // add copy to ChangedProjectData
                // don't duplicate if already had another update prior to push
                var alreadyQueued = _dataState.GetChangedContextData().Where(
                    x => x.Id == workingProject.Id);
                if (!alreadyQueued.Any()) { _dataState.GetChangedProjectData().Add(workingProject.Clone()); }
            }
        }

        // updates local "working" copy of project data, for use after sync
        private void UpdateWorkingProjectsFromDataState()
        {
            List<ProjectDisplayModel> workingProjectList = _dataState.GetProjects()!.ConvertAll(project => project.Clone());
            _dataState.SetWorkingProjects(workingProjectList);
        }

        // ensure one update call at a time
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

            UpdateWorkingProjectsFromDataState();

            _dataState.InvokeDataStateChanged(nameof(EDataRefreshType.Projects));
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
            newDisplayProject.OrderIndex = _dataState.GetProjects()!.Count;
            // give temp local tracking id
            var currentTempId = _dataState.GetTempProjectId();
            newDisplayProject.TempLocalId = _dataState.SetTempProjectId(++currentTempId);

            // flag for sync
            newDisplayProject.ClientLastUpdated = DateTimeOffset.Now;
            _dataState.GetChangedProjectData().Add(newDisplayProject.Clone());

            // update local data state
            _dataState.GetWorkingProjects()!.Add(newDisplayProject.Clone());
            _dataState.GetProjects()!.Add(newDisplayProject.Clone());

            // trigger UI update + request sync
            _dataState.InvokeDataStateChanged(nameof(EDataRefreshType.Projects));
            InvokeSyncRequest(nameof(AddProject));                           

            return newDisplayProject;
        }

        // validates request, processes local delete, flags for sync, refreshes UI
        public async Task DeleteProject(ProjectDisplayModel workingProject)
        {
            // map from ProjectDisplayModel to ProjectModel
            ProjectModel project = _mapper.Map<ProjectModel>(workingProject);

            // ensure other projects have updated OrderIndex values
            this.ShiftCollectionOrderIndices(workingProject, _dataState.GetWorkingProjects()!.ToList());
            // ^ this updates those tasks' orderIndex value, but doesn't flag for sync / do additional processing
            // that will be caught and processed in the next step when UpdateTaskData is called

            // handle project's tasks - remove assigned project
            List<TaskDisplayModel> projectTasks = _dataState.GetWorkingTasks()!.Where(x => x.ProjectId == project.Id).ToList();
            foreach (TaskDisplayModel task in projectTasks)
            {
                task.ProjectName = null;
                UpdateTaskData(task);
            }

            // update local data state
            if (project.Id == null) // never existed on server; insert was pending push
            {
                // local delete
                var dataStateProject = _dataState.GetProjects()!.Find(x => x.TempLocalId == workingProject.TempLocalId);
                if (dataStateProject != null) _dataState.GetProjects()!.Remove(dataStateProject);
                var changedProject = _dataState.GetChangedContextData().Find(x => x.TempLocalId == workingProject.TempLocalId);
                if (changedProject != null) _dataState.GetChangedContextData().Remove(changedProject);
                _dataState.GetWorkingProjects()!.Remove(workingProject);              
            }
            else
            {
                ProjectDisplayModel dataStateProject = _dataState.GetProjects()!.Find(x => x.Id == project.Id)!;
                // flag for server delete on sync
                dataStateProject.Deleted = DateTimeOffset.Now;
                dataStateProject.ClientLastUpdated = DateTimeOffset.Now;

                // if had an update pending push, don't add a duplicate to changedProjectData
                var alreadyQueued = _dataState.GetChangedContextData().Where(
                    x => x.Id == dataStateProject.Id);
                if (alreadyQueued.Count() == 0) { _dataState.GetChangedProjectData().Add(dataStateProject.Clone()); }

                // local delete
                if (dataStateProject != null) _dataState.GetProjects()!.Remove(dataStateProject);
                _dataState.GetWorkingProjects()!.Remove(workingProject);
            }

            // trigger UI update
            _dataState.InvokeDataStateChanged(nameof(EDataRefreshType.Projects));
        }

        // validates request, performs additional processing, flags for sync, refreshes UI
        public async Task UpdateProjectData(ProjectDisplayModel workingProject)
        {
            var compareResult = _dataHelper.HasProjectDataChanged(workingProject);

            if (compareResult.HasChanged)
            {
                if (!_dataHelper.IsUpdatedProjectNameUnique(workingProject))
                {
                    LogError("Unable to update project: project names must be unique.");
                    return;
                }

                if (!IsProjectCurrentlyBeingUpdated(workingProject))
                {
                    // unlock
                    Interlocked.Exchange(ref _projectUpdateEntered, 0);
                    _projectBeingUpdated = null;                 
                }

                // lock, to prevent other property changes during processing from triggering new Update calls
                if (Interlocked.Increment(ref _projectUpdateEntered) != 1) { return; }
                _projectBeingUpdated = workingProject;

                await ProcessLocalProjectUpdate(workingProject, compareResult.CollectionNameChanged);

                // unlock
                Interlocked.Exchange(ref _projectUpdateEntered, 0);
                _projectBeingUpdated = null;
                // trigger UI update
                _dataState.InvokeDataStateChanged("Projects");
            }
        }
    }
}
