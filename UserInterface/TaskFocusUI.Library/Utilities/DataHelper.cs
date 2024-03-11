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
                    taskA.DueDate == taskB.DueDate;
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
    }
}
