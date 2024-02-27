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
        public DateTime? DateCompleted { get; set; }
        public int? ProjectId { get; set; }
        public int? ContextId { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public void CallPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        // user-editable properties
        // ====================
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

        private bool _completed = false;
        public bool Completed
        {
            get {  return _completed; }
            set
            {
                _completed = value;
                CallPropertyChanged(nameof(Completed));
            }
        }

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

        private string _contextName;
        public string ContextName
        {
            get { return _contextName; }
            set
            {
                _contextName = value;
                CallPropertyChanged(nameof(ContextName));
            }
        }

        private DateTime? _dueDate;
        public DateTime? DueDate
        {
            get { return _dueDate; }
            set
            {
                _dueDate = value;
                CallPropertyChanged(nameof(DueDate));
            }
        }
    }
}
