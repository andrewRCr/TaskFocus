using System.Collections.Generic;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Utilities
{
    public interface IDataHelper
    {
        List<TaskModel> TasksLastFetch { get; set; }
        bool HasTaskContextNameChanged(TaskModel frontEndTask);
        bool HasTaskDataChanged(TaskModel frontEndTask);
        bool HasTaskProjectNameChanged(TaskModel frontEndTask);
        bool IsTaskDueOrOverDue(TaskModel frontEndTask);
    }
}