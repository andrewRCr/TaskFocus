using System;

namespace TaskFocusUI.Library.Models
{
    public class TaskModel
    {
        public int? Id { get; set; }
        public string UserId { get; set; }
        public string TaskName { get; set; }
        public bool Completed { get; set; } = false;
        public DateTime? DateCompleted { get; set; }
        public int? ProjectId { get; set; }
        public string ProjectName { get; set; }
        public int? ContextId { get; set; }
        public string ContextName { get; set; }
        public DateTime? DueDate { get; set; }
        public int? InboxIndex { get; set; }
        public int? ProjectIndex { get; set; }
        public int? ContextIndex { get; set; }
        public bool Starred { get; set; } = false;
        public int? TodayIndex { get; set; }
        public bool CleanedUp { get; set; } = false;
        // for sync
        public DateTimeOffset ServerLastUpdated { get; set; }
        public DateTimeOffset ClientLastUpdated { get; set; }
        public DateTimeOffset? Deleted { get; set; }
    }
}
