using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Utilities
{
    public class DataHelper : IDataHelper
    {
        public List<TaskModel> TasksLastFetch { get; set; }
        public List<ProjectModel> ProjectsLastFetch { get; set; }
        public List<ContextModel> ContextsLastFetch { get; set; }
        public UserSettingsModel UserSettingsLastFetch { get; set; }

        public bool HasTaskDataChanged(TaskModel frontEndTask)
        {
            TaskModel taskLastFetch = TasksLastFetch.Find(x => x.Id == frontEndTask.Id);

            bool IsDataEqual(TaskModel taskA, TaskModel taskB)
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

            return !IsDataEqual(frontEndTask, taskLastFetch);
        }

        public bool HasTaskProjectNameChanged(TaskModel frontEndTask)
        {
            TaskModel taskLastFetch = TasksLastFetch.Find(x => x.Id == frontEndTask.Id);
            return frontEndTask.ProjectName != taskLastFetch.ProjectName;
        }

        public bool HasTaskContextNameChanged(TaskModel frontEndTask)
        {
            TaskModel taskLastFetch = TasksLastFetch.Find(x => x.Id == frontEndTask.Id);
            return frontEndTask.ContextName != taskLastFetch.ContextName;
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

        public bool HasContextDataChanged(ContextModel frontEndContext)
        {
            ContextModel contextLastFetch = ContextsLastFetch.Find(x => x.Id == frontEndContext.Id);

            bool IsDataEqual(ContextModel contextA, ContextModel contextB)
            {
                return JsonConvert.SerializeObject(contextA) == JsonConvert.SerializeObject(contextB);
            }

            return !IsDataEqual(frontEndContext, contextLastFetch);
        }

        public bool HasSettingsDataChanged(UserSettingsModel frontEndSettings)
        {
            bool IsDataEqual(UserSettingsModel settingsA, UserSettingsModel settingsB)
            {
                return JsonConvert.SerializeObject(settingsA) == JsonConvert.SerializeObject(settingsB);
            }

            return !IsDataEqual(frontEndSettings, UserSettingsLastFetch);
        }
    }
}
