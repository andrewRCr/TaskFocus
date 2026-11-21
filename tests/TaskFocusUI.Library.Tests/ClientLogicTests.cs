using TaskFocusUI.Library.Data.State;
using TaskFocusUI.Library.Data.Utilities;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Tests
{
    public sealed class ClientLogicTests
    {
        [Theory]
        [InlineData(null, false)]
        [InlineData(-1, true)]
        [InlineData(0, true)]
        [InlineData(7, false)]
        public void DueOrOverdueUsesTheLocalCalendarDate(int? daysFromToday, bool expected)
        {
            var helper = new DataHelper(null!, new DataState());
            var task = new TaskDisplayModel
            {
                DueDate = daysFromToday.HasValue ? DateTime.Today.AddDays(daysFromToday.Value) : null
            };

            Assert.Equal(expected, helper.IsTaskDueOrOverDue(task));
        }

        [Fact]
        public void TaskClonePreservesSyncStateAndIsIndependent()
        {
            var original = new TaskDisplayModel
            {
                TaskName = "Draft report",
                TempLocalId = 42,
                InboxIndex = 3,
                ServerLastUpdated = DateTimeOffset.UnixEpoch.AddDays(1),
                ClientLastUpdated = DateTimeOffset.UnixEpoch.AddDays(2)
            };

            var clone = original.Clone();

            Assert.NotSame(original, clone);
            Assert.Equal(original.TaskName, clone.TaskName);
            Assert.Equal(original.TempLocalId, clone.TempLocalId);
            Assert.Equal(original.InboxIndex, clone.InboxIndex);
            Assert.Equal(original.ServerLastUpdated, clone.ServerLastUpdated);
            Assert.Equal(original.ClientLastUpdated, clone.ClientLastUpdated);
            clone.TaskName = "Revised report";
            Assert.Equal("Draft report", original.TaskName);
        }

        [Fact]
        public void ProjectClonePreservesContextAndOrder()
        {
            var original = new ProjectDisplayModel
            {
                Id = 10,
                ProjectName = "Launch",
                ContextId = 7,
                ContextName = "Work",
                OrderIndex = 2
            };

            var clone = original.Clone();

            Assert.NotSame(original, clone);
            Assert.Equal(original.Id, clone.Id);
            Assert.Equal(original.ProjectName, clone.ProjectName);
            Assert.Equal(original.ContextId, clone.ContextId);
            Assert.Equal(original.ContextName, clone.ContextName);
            Assert.Equal(original.OrderIndex, clone.OrderIndex);
        }

        [Fact]
        public void SettingsClonePreservesCustomCleanupPreferences()
        {
            var original = new UserSettingsDisplayModel
            {
                CleanUpImmediately = true,
                CleanUpDelayDays = 2,
                DeleteDelayDays = 12
            };

            var clone = original.Clone();

            Assert.NotSame(original, clone);
            Assert.True(clone.CleanUpImmediately);
            Assert.Equal(2, clone.CleanUpDelayDays);
            Assert.Equal(12, clone.DeleteDelayDays);
        }

        [Fact]
        public void ContextIndexerReadsAndWritesTheOrderIndex()
        {
            var context = new ContextDisplayModel { OrderIndex = 3 };

            Assert.Equal(3, context[nameof(ContextDisplayModel.OrderIndex)]);

            context[nameof(ContextDisplayModel.OrderIndex)] = 5;

            Assert.Equal(5, context.OrderIndex);
        }
    }
}
