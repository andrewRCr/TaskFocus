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
    }
}
