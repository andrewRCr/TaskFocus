using System;

namespace TaskFocusAPI.Library.Models
{
    public class ProjectModel
    {
        public int? Id { get; set; }
        public string UserId { get; set; }
        public string ProjectName { get; set; }
        public int? ContextId { get; set; }
        public string ContextName { get; set; }
        public DateTime? DueDate { get; set; }
        public bool Completed { get; set; } = false;
        public DateTime? DateCompleted { get; set; }
        public int? OrderIndex { get; set; }
        // for sync
        public DateTimeOffset ServerLastUpdated { get; set; }
        public DateTimeOffset ClientLastUpdated { get; set; }
    }
}
