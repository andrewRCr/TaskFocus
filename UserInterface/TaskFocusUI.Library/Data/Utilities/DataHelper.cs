using AutoMapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using TaskFocusUI.Library.Data.State;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Data.Utilities
{
    public struct TaskDataCompareResult
    {
        public bool HasChanged;
        public bool ProjectNameChanged;
        public bool ContextNameChanged;
        public bool IndicesOnly;
    }

    public struct ProjectDataCompareResult
    {
        public bool HasChanged;
        public bool ProjectNameChanged;
    }

    public struct DataSyncResult
    {
        public int numRowsInserted = 0;
        public int numRowsDeleted = 0;
        public int numRowsUpdated = 0;

        public DataSyncResult() { }
    }

    public class DataHelper : IDataHelper
    {
        private IMapper _mapper;
        private IDataState _dataState;

        //public List<TaskModel>? TasksLastFetch { get; set; }
        //public List<ProjectModel>? ProjectsLastFetch { get; set; }
        //public List<ContextModel>? ContextsLastFetch { get; set; }
        public UserSettingsModel? UserSettingsLastFetch { get; set; }

        // TODO: do these being located here (in this class) make sense? 
        public ProjectDisplayModel? FocusedProject { get; set; }
        public List<TaskDisplayModel>? FocusedProjectTasks { get; set; }
        public ContextDisplayModel? FocusedContext { get; set; }
        public List<TaskDisplayModel>? FocusedContextTasks { get; set; }

        public DataHelper(IMapper mapper, IDataState dataState)
        {
            _mapper = mapper;
            _dataState = dataState;
        }

        public TaskDataCompareResult HasTaskDataChanged(TaskDisplayModel displayTask)
        {
            TaskModel compareAgainstTask;

            if (displayTask.Id == null && displayTask.TempLocalId != null)
            {
                TaskDisplayModel unpushedTask = _dataState.ChangedTaskData.Find(
                    x => x.TempLocalId == displayTask.TempLocalId)!;
                compareAgainstTask = _mapper.Map<TaskModel>(unpushedTask);
            }
            else
            {
                //TaskModel taskLastFetch = TasksLastFetch!.Find(x => x.Id == displayTask.Id)!;
                //compareAgainstTask = taskLastFetch;

                TaskDisplayModel dataStateTask = _dataState.GetTasks()!.Find(
                    x => x.Id == displayTask.Id)!;
                compareAgainstTask = _mapper.Map<TaskModel>(dataStateTask);
            }

            static bool AreUserEditablePropertiesEqual(TaskModel taskA, TaskModel taskB)
            {
                return AreNonIndexPropertiesEqual(taskA, taskB) && AreIndexPropertiesEqual(taskA, taskB);
            }

            static bool AreNonIndexPropertiesEqual(TaskModel taskA, TaskModel taskB)
            {
                return taskA.TaskName == taskB.TaskName &&
                       taskA.Completed == taskB.Completed &&
                       taskA.ProjectName == taskB.ProjectName &&
                       taskA.ContextName == taskB.ContextName &&
                       taskA.DueDate == taskB.DueDate &&
                       taskA.Starred == taskB.Starred &&
                       taskA.CleanedUp == taskB.CleanedUp;
            }

            static bool AreIndexPropertiesEqual(TaskModel taskA, TaskModel taskB)
            {
                return taskA.InboxIndex == taskB.InboxIndex &&
                       taskA.ProjectIndex == taskB.ProjectIndex &&
                       taskA.ContextIndex == taskB.ContextIndex &&
                       taskA.TodayIndex == taskB.TodayIndex;
            }

            TaskModel task = _mapper.Map<TaskModel>(displayTask);
            bool hasChanged = !AreUserEditablePropertiesEqual(task, compareAgainstTask);
            //Console.WriteLine($"{task.TaskName} hasChanged: {hasChanged}");
            //Console.WriteLine($"passedTask: {task.TaskName} compareAgainstTask: {compareAgainstTask.TaskName}");

            return new()
            {
                HasChanged = hasChanged,
                ProjectNameChanged = displayTask.ProjectName != compareAgainstTask.ProjectName,
                ContextNameChanged = displayTask.ContextName != compareAgainstTask.ContextName,
                IndicesOnly = AreNonIndexPropertiesEqual(task, compareAgainstTask) && hasChanged
            };
        }

        public bool IsTaskDueOrOverDue(TaskDisplayModel frontEndTask)
        {
            if (frontEndTask.DueDate != null)
            {
                return frontEndTask.DueDate <= DateTime.Now.Date;
            }

            return false;
        }

        public ProjectDataCompareResult HasProjectDataChanged(ProjectDisplayModel displayProject)
        {
            ProjectModel compareAgainstProject;

            if (displayProject.Id == null && displayProject.TempLocalId != null)
            {
                ProjectDisplayModel unpushedProject = _dataState.ChangedProjectData!.Find(
                    x => x.TempLocalId == displayProject.TempLocalId)!;
                compareAgainstProject = _mapper.Map<ProjectModel>(unpushedProject);
            }
            else
            {
                //ProjectModel projectLastFetch = ProjectsLastFetch!.Find(x => x.Id == displayProject.Id)!;
                ProjectDisplayModel dataStateProject = _dataState.GetProjects()!.Find(x => x.Id == displayProject.Id)!;
                compareAgainstProject = _mapper.Map<ProjectModel>(dataStateProject);
            }

            static bool AreUserEditablePropertiesEqual(ProjectModel projectA, ProjectModel projectB)
            {
                return projectA.ProjectName == projectB.ProjectName &&
                       projectA.OrderIndex == projectB.OrderIndex;
            }

            //return !AreUserEditablePropertiesEqual(_mapper.Map<ProjectModel>(displayProject), compareAgainstProject);

            return new()
            {
                HasChanged = !AreUserEditablePropertiesEqual(_mapper.Map<ProjectModel>(displayProject), compareAgainstProject),
                ProjectNameChanged = displayProject.ProjectName != compareAgainstProject.ProjectName
            };
        }

        public bool IsNewProjectNameUnique(string proposedProjectName)
        {
            var unpushedProjects = _dataState.ChangedProjectData.Where(
                x => x.Id == null && x.TempLocalId != null);

            if (_dataState.GetProjects()!.Count == 0 && !unpushedProjects.Any()) { return true; }

            foreach (ProjectDisplayModel project in _dataState.GetProjects()!)
            {
                if (project.ProjectName.ToLower() == proposedProjectName.ToLower()) { return false; }
            }
            foreach (ProjectDisplayModel project in unpushedProjects)
            {
                if (project.ProjectName.ToLower() == proposedProjectName.ToLower()) { return false; }
            }

            return true;
        }

        public bool IsUpdatedProjectNameUnique(ProjectDisplayModel updatedDisplayProject)
        {
            var unpushedProjects = _dataState.ChangedProjectData.Where(
    x => x.Id == null && x.TempLocalId != null);

            foreach (ProjectDisplayModel project in _dataState.GetProjects()!)
            {
                if (project.Id == updatedDisplayProject.Id) { continue; }
                if (project.ProjectName.ToLower() == updatedDisplayProject.ProjectName.ToLower())
                {
                    return false;
                }
            }

            foreach (ProjectDisplayModel project in unpushedProjects)
            {
                if (project.TempLocalId == updatedDisplayProject.TempLocalId) { continue; }
                if (project.ProjectName.ToLower() == updatedDisplayProject.ProjectName.ToLower())
                {
                    return false;
                }
            }

            return true;
        }

        public bool HasContextDataChanged(ContextDisplayModel displayContext)
        {
            ContextDisplayModel compareAgainstContext;

            if (displayContext.Id == null && displayContext.TempLocalId != null)
            {
                ContextDisplayModel unpushedContext = _dataState.ChangedContextData!.Find(
                    x => x.TempLocalId == displayContext.TempLocalId)!;
                compareAgainstContext = unpushedContext;
            }
            else
            {
                ContextDisplayModel dataStateContext = _dataState.GetContexts()!.Find(x => x.Id == displayContext.Id)!;
                compareAgainstContext = dataStateContext;
            }

            static bool AreUserEditablePropertiesEqual(ContextDisplayModel contextA, ContextDisplayModel contextB)
            {
                return contextA.ContextName == contextB.ContextName &&
                       contextA.OrderIndex == contextB.OrderIndex;
            }

            return !AreUserEditablePropertiesEqual(displayContext, compareAgainstContext);
        }


        public bool IsNewContextNameUnique(string proposedContextName)
        {
            foreach (ContextDisplayModel context in _dataState.GetContexts()!)
            {
                if (context.ContextName.ToLower() == proposedContextName.ToLower())
                {
                    return false;
                }
            }

            return true;
        }

        public bool IsUpdatedContextNameUnique(ContextModel updatedFrontEndContext)
        {
            foreach (ContextDisplayModel context in _dataState.GetContexts()!)
            {
                if (context.Id == updatedFrontEndContext.Id) { continue; }
                if (context.ContextName.ToLower() == updatedFrontEndContext.ContextName.ToLower())
                {
                    return false;
                }
            }

            return true;
        }

        public bool HasSettingsDataChanged(UserSettingsModel frontEndSettings)
        {
            bool IsDataEqual(UserSettingsModel settingsA, UserSettingsModel settingsB)
            {
                return JsonConvert.SerializeObject(settingsA) == JsonConvert.SerializeObject(settingsB);
            }

            return !IsDataEqual(frontEndSettings, UserSettingsLastFetch);
        }

        public DataSyncResult CombineSyncResults(DataSyncResult resultA, DataSyncResult resultB)
        {
            DataSyncResult combinedResult;
            combinedResult.numRowsInserted = resultA.numRowsInserted + resultB.numRowsInserted;
            combinedResult.numRowsDeleted = resultA.numRowsDeleted + resultB.numRowsDeleted;
            combinedResult.numRowsUpdated = resultA.numRowsUpdated + resultB.numRowsUpdated;

            return combinedResult;
        }

        public bool SyncChangesDetected(DataSyncResult result)
        {
            return result.numRowsInserted != 0 || result.numRowsDeleted != 0 || result.numRowsUpdated != 0;
        }
    }
}
