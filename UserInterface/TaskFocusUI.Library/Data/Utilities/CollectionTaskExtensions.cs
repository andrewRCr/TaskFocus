using System.Collections.Generic;
using System.Linq;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Data.Utilities
{
    public static class CollectionTaskExtensions
    {
        public static List<TaskDisplayModel> GetProjectTasks(this IEnumerable<TaskDisplayModel> tasks, int id) =>
            tasks.Where(task => task.ProjectId == id && !task.CleanedUp)
                 .OrderBy(task => task.ProjectIndex).ToList();

        public static List<TaskDisplayModel> GetContextTasks(this IEnumerable<TaskDisplayModel> tasks, int id) =>
            tasks.Where(task => task.ContextId == id && !task.CleanedUp)
                 .OrderBy(task => task.ContextIndex).ToList();
    }
}
