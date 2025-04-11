using AutoMapper;
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

    public struct CollectionDataCompareResult
    {
        public bool HasChanged;
        public bool CollectionNameChanged;
    }

    public struct DataSyncResult
    {
        public int numRowsInserted = 0;
        public int numRowsDeleted = 0;
        public int numRowsUpdated = 0;

        public DataSyncResult() {}
    }

    public partial class DataHelper : IDataHelper
    {
        public DataHelper(IMapper mapper, IDataState dataState)
        {
            _mapper = mapper;
            _dataState = dataState;
        }

        private IMapper _mapper;
        private IDataState _dataState;

        // tracking front-end data focus
        // note: on interface (IDataHelper), these setters are internal (i.e., front-end read-only)
        public ProjectDisplayModel? FocusedProject { get; set; }
        public List<TaskDisplayModel>? FocusedProjectTasks { get; set; }
        public ContextDisplayModel? FocusedContext { get; set; }
        public List<TaskDisplayModel>? FocusedContextTasks { get; set; }

        // task helpers
        // ====================

        public TaskDataCompareResult HasTaskDataChanged(TaskDisplayModel displayTask)
        {
            TaskModel compareAgainstTask;

            if (displayTask.Id == null && displayTask.TempLocalId != null)
            {
                TaskDisplayModel unpushedTask = _dataState.GetChangedTaskData().Find(
                    x => x.TempLocalId == displayTask.TempLocalId)!;
                compareAgainstTask = _mapper.Map<TaskModel>(unpushedTask);
            }
            else
            {
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

        // project helpers
        // ====================

        public CollectionDataCompareResult HasProjectDataChanged(ProjectDisplayModel displayProject)
        {
            ProjectModel compareAgainstProject;

            if (displayProject.Id == null && displayProject.TempLocalId != null)
            {
                ProjectDisplayModel unpushedProject = _dataState.GetChangedProjectData().Find(
                    x => x.TempLocalId == displayProject.TempLocalId)!;
                compareAgainstProject = _mapper.Map<ProjectModel>(unpushedProject);
            }
            else
            {
                ProjectDisplayModel dataStateProject = _dataState.GetProjects()!.Find(x => x.Id == displayProject.Id)!;
                compareAgainstProject = _mapper.Map<ProjectModel>(dataStateProject);
            }

            static bool AreUserEditablePropertiesEqual(ProjectModel projectA, ProjectModel projectB)
            {
                return projectA.ProjectName == projectB.ProjectName &&
                       projectA.OrderIndex == projectB.OrderIndex;
            }

            return new()
            {
                HasChanged = !AreUserEditablePropertiesEqual(_mapper.Map<ProjectModel>(displayProject), compareAgainstProject),
                CollectionNameChanged = displayProject.ProjectName != compareAgainstProject.ProjectName
            };
        }

        public bool IsNewProjectNameUnique(string proposedProjectName)
        {
            var unpushedProjects = _dataState.GetChangedProjectData().Where(
                x => x.Id == null && x.TempLocalId != null);

            if (_dataState.GetProjects()!.Count == 0 && !unpushedProjects.Any()) return true;

            foreach (ProjectDisplayModel project in _dataState.GetProjects()!)
            {
                if (project.ProjectName.ToLower() == proposedProjectName.ToLower()) return false;
            }
            foreach (ProjectDisplayModel project in unpushedProjects)
            {
                if (project.ProjectName.ToLower() == proposedProjectName.ToLower()) return false;
            }

            return true;
        }

        public bool IsUpdatedProjectNameUnique(ProjectDisplayModel updatedDisplayProject)
        {
            var unpushedProjects = _dataState.GetChangedProjectData().Where(
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

        // context helpers
        // ====================

        public CollectionDataCompareResult HasContextDataChanged(ContextDisplayModel displayContext)
        {
            ContextDisplayModel compareAgainstContext;

            if (displayContext.Id == null && displayContext.TempLocalId != null)
            {
                ContextDisplayModel unpushedContext = _dataState.GetChangedContextData().Find(
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

            return new()
            {
                HasChanged = !AreUserEditablePropertiesEqual(displayContext, compareAgainstContext),
                CollectionNameChanged = displayContext.ContextName != compareAgainstContext.ContextName
            };
        }

        public bool IsNewContextNameUnique(string proposedContextName)
        {
            var unpushedContexts = _dataState.GetChangedContextData().Where(
                x => x.Id == null && x.TempLocalId != null);

            if (_dataState.GetContexts()!.Count == 0 && !unpushedContexts.Any()) return true;

            foreach (ContextDisplayModel context in _dataState.GetContexts()!)
            {
                if (context.ContextName.ToLower() == proposedContextName.ToLower()) return false;
            }
            foreach (ContextDisplayModel context in unpushedContexts)
            {
                if (context.ContextName.ToLower() == proposedContextName.ToLower()) return false;
            }

            return true;
        }

        public bool IsUpdatedContextNameUnique(ContextDisplayModel updatedDisplayContext)
        {
            var unpushedContexts = _dataState.GetChangedContextData().Where(
    x => x.Id == null && x.TempLocalId != null);

            foreach (ContextDisplayModel context in _dataState.GetContexts()!)
            {
                if (context.Id == updatedDisplayContext.Id) { continue; }
                if (context.ContextName.ToLower() == updatedDisplayContext.ContextName.ToLower())
                {
                    return false;
                }
            }

            foreach (ContextDisplayModel context in unpushedContexts)
            {
                if (context.TempLocalId == updatedDisplayContext.TempLocalId) { continue; }
                if (context.ContextName.ToLower() == updatedDisplayContext.ContextName.ToLower())
                {
                    return false;
                }
            }

            return true;
        }

        // user data helpers
        // ====================

        public bool HasSettingsDataChanged(UserSettingsDisplayModel workingSettings)
        {
            static bool AreUserEditablePropertiesEqual(UserSettingsDisplayModel settingsA, UserSettingsDisplayModel settingsB)
            {
                return settingsA.CleanUpImmediately == settingsB.CleanUpImmediately &&
                       settingsA.CleanUpDelayDays == settingsB.CleanUpDelayDays &&
                       settingsA.DeleteDelayDays == settingsB.DeleteDelayDays;
            }

            return !AreUserEditablePropertiesEqual(workingSettings, _dataState.GetUserSettings()!);
        }

        public bool HasUserDataChanged(UserDisplayModel workingUser)
        {
            static bool AreUserEditablePropertiesEqual(UserDisplayModel userDataA, UserDisplayModel userDataB)
            {
                return userDataA.FirstName == userDataB.FirstName &&
                       userDataA.LastName == userDataB.LastName;
            }

            return !AreUserEditablePropertiesEqual(workingUser, _dataState.GetCurrentUser()!);
        }

        // synchronization helpers
        // ====================

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
