using TaskFocusUI.Library.Data.Utilities;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Tests
{
    public sealed class CollectionTaskTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void FocusedRowsUseWorkingInstancesAndIncludePendingInserts(bool project)
        {
            var pending = new TaskDisplayModel
            {
                TempLocalId = 1,
                TaskName = "Draft report",
                ProjectId = 10,
                ContextId = 10
            };
            var tasks = new[] { pending };

            Assert.Same(pending, Assert.Single(GetFocusedRows(tasks, 10, project)));
            pending.TaskName = "Revised report";

            var row = Assert.Single(GetFocusedRows(tasks, 10, project));
            row.Starred = true;

            Assert.Same(pending, row);
            Assert.Equal("Revised report", pending.TaskName);
            Assert.True(pending.Starred);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void FocusedRowsExcludeCleanedTasksAndUseTheSelectedCollectionRank(bool project)
        {
            var first = new TaskDisplayModel { ProjectId = 10, ContextId = 10, ProjectIndex = 1, ContextIndex = 2 };
            var second = new TaskDisplayModel { ProjectId = 10, ContextId = 10, ProjectIndex = 2, ContextIndex = 1 };
            var cleaned = new TaskDisplayModel { ProjectId = 10, ContextId = 10, CleanedUp = true };
            var otherProject = new TaskDisplayModel { ProjectId = 20, ContextId = 10, ProjectIndex = 0, ContextIndex = 0 };
            var otherContext = new TaskDisplayModel { ProjectId = 10, ContextId = 20, ProjectIndex = 0, ContextIndex = 0 };
            var tasks = new[] { second, cleaned, otherProject, first, otherContext };

            Assert.Equal(
                project ? new[] { otherContext, first, second } : new[] { otherProject, second, first },
                GetFocusedRows(tasks, 10, project));
            Assert.Empty(GetFocusedRows(tasks, 30, project));
        }

        private static List<TaskDisplayModel> GetFocusedRows(TaskDisplayModel[] tasks, int id, bool project) =>
            project ? tasks.GetProjectTasks(id) : tasks.GetContextTasks(id);
    }
}
