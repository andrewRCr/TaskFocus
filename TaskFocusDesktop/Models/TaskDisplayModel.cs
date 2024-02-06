using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskFocusDesktop.Models
{
    public class TaskDisplayModel : INotifyPropertyChanged
    {
        public int? Id { get; set; }
        public string UserId { get; set; }

        private string _taskName;
        public string TaskName
        {
            get { return _taskName; }
            set
            {
                _taskName = value;
                CallPropertyChanged(nameof(TaskName));
            }
        }

        public bool Completed { get; set; } = false;
        public DateTime? DateCompleted { get; set; }
        public int? ProjectId { get; set; }

        private string _projectName;
        public string ProjectName
        {
            get { return _projectName; }
            set 
            { 
                _projectName = value; 
                CallPropertyChanged(nameof(ProjectName));
            }
        }

        public int? ContextId { get; set; }
        public string ContextName { get; set; }
        public DateTime? DueDate { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public void CallPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
