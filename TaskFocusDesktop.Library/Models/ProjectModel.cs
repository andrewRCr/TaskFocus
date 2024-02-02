using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskFocusDesktop.Library.Models
{
    public class ProjectModel
    {
        public int Id { get; set; }
        public string ProjectName { get; set; }
        public int ContextId { get; set; }
        public string ContextName { get; set; }
        public DateTime DueDate { get; set; }
    }
}
