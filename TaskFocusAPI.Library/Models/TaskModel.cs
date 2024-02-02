using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskFocusAPI.Library.Models
{
    public class TaskModel
    {
        public int Id { get; set; }
        public string TaskName { get; set; }
        public bool Completed { get; set; }
        public DateTime DateCompleted { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public int ContextId { get; set; }
        public string ContextName { get; set; }
        public DateTime DueDate { get; set; }
    }
}
