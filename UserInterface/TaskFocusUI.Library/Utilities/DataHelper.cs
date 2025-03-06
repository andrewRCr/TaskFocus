using AutoMapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Utilities
{
    public struct TaskDataCompareResult
    {
        public bool HasChanged;
        public bool ProjectNameChanged;
        public bool ContextNameChanged;
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

        public List<TaskModel>? TasksLastFetch { get; set; }
        public List<ProjectModel>? ProjectsLastFetch { get; set; }
        public List<ContextModel>? ContextsLastFetch { get; set; }
        public UserSettingsModel? UserSettingsLastFetch { get; set; }

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
                TaskDisplayModel unpushedTask = _dataState.ChangedTaskData.Find(x => x.TempLocalId == displayTask.TempLocalId)!;
                compareAgainstTask = _mapper.Map<TaskModel>(unpushedTask);
            }
            else
            {
                TaskModel taskLastFetch = TasksLastFetch!.Find(x => x.Id == displayTask.Id)!;
                compareAgainstTask = taskLastFetch;
            }

            static bool AreUserEditablePropertiesEqual(TaskModel taskA, TaskModel taskB)
            {
                return taskA.TaskName == taskB.TaskName &&
                       taskA.Completed == taskB.Completed &&
                       taskA.ProjectName == taskB.ProjectName &&
                       taskA.ContextName == taskB.ContextName &&
                       taskA.DueDate == taskB.DueDate &&
                       taskA.InboxIndex == taskB.InboxIndex &&
                       taskA.ProjectIndex == taskB.ProjectIndex &&
                       taskA.ContextIndex == taskB.ContextIndex &&
                       taskA.Starred == taskB.Starred &&
                       taskA.TodayIndex == taskB.TodayIndex &&
                       taskA.CleanedUp == taskB.CleanedUp;
            }

            return new() {
                HasChanged = !AreUserEditablePropertiesEqual(_mapper.Map<TaskModel>(displayTask), compareAgainstTask),
                ProjectNameChanged = displayTask.ProjectName != compareAgainstTask.ProjectName,
                ContextNameChanged = displayTask.ContextName != compareAgainstTask.ContextName
            };
        }

        public bool IsTaskDueOrOverDue(TaskModel frontEndTask)
        {
            if (frontEndTask.DueDate != null)
            {
                return frontEndTask.DueDate <= DateTime.Now.Date;
            }

            return false;
        }

        public bool HasProjectDataChanged(ProjectModel frontEndProject)
        {
            ProjectModel projectLastFetch = ProjectsLastFetch.Find(x => x.Id == frontEndProject.Id);

            bool IsDataEqual(ProjectModel projectA, ProjectModel projectB)
            {
                return JsonConvert.SerializeObject(projectA) == JsonConvert.SerializeObject(projectB);
            }

            return !IsDataEqual(frontEndProject, projectLastFetch);
        }

        public bool IsNewProjectNameUnique(string proposedProjectName)
        {
            foreach (ProjectModel project in ProjectsLastFetch)
            {
                if (project.ProjectName.ToLower() == proposedProjectName.ToLower())
                {
                    return false;
                }
            }

            return true;
        }

        public bool IsUpdatedProjectNameUnique(ProjectModel updatedFrontEndProject)
        {
            foreach (ProjectModel project in ProjectsLastFetch)
            {
                if (project.Id == updatedFrontEndProject.Id) { continue; }
                if (project.ProjectName.ToLower() == updatedFrontEndProject.ProjectName.ToLower())
                {
                    return false;
                }
            }

            return true;
        }

        public bool HasContextDataChanged(ContextModel frontEndContext)
        {
            ContextModel contextLastFetch = ContextsLastFetch.Find(x => x.Id == frontEndContext.Id);

            bool IsDataEqual(ContextModel contextA, ContextModel contextB)
            {
                return JsonConvert.SerializeObject(contextA) == JsonConvert.SerializeObject(contextB);
            }

            return !IsDataEqual(frontEndContext, contextLastFetch);
        }


        public bool IsNewContextNameUnique(string proposedContextName)
        {
            foreach (ContextModel context in ContextsLastFetch)
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
            foreach (ContextModel context in ContextsLastFetch)
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
