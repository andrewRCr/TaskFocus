using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskFocusUI.Library.Models
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
    }
}
