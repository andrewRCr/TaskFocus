using System;
using System.ComponentModel;
using System.Reflection;

namespace TaskFocusUI.Library.Models
{
    public class TaskDisplayModel : INotifyPropertyChanged
    {
        public int? Id { get; set; }
        public string UserId { get; set; }
        public DateTime? DateCompleted { get; set; }
        public int? ProjectId { get; set; }
        public int? ContextId { get; set; }

        // indexer
        public object this[string propertyName]
        {
            get
            {
                var properties = typeof(TaskDisplayModel)
                        .GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var property in properties)
                {
                    if (property.Name == propertyName && property.CanRead)
                        return property.GetValue(this, null);
                }

                throw new ArgumentException($"Can't find property {propertyName}!");

            }
            set 
            {
                Type myType = typeof(TaskDisplayModel);
                PropertyInfo myPropInfo = myType.GetProperty(propertyName);
                myPropInfo.SetValue(this, value, null);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void CallPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // directly editable (by user or app) properties
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
            get { return _completed; }
            set
            {
                _completed = value;
                CallPropertyChanged(nameof(Completed));
            }
        }

        private bool _cleanedUp = false;
        public bool CleanedUp
        {
            get { return _cleanedUp; }
            set
            {
                _cleanedUp = value;
                CallPropertyChanged(nameof(CleanedUp));
            }
        }

        private bool _starred = false;
        public bool Starred
        {
            get { return _starred; }
            set
            {
                _starred = value;
                CallPropertyChanged(nameof(Starred));
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
                //Debug.WriteLine($"{TaskName}'s property ProjectName changed to {ProjectName}");
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

        private int? _inboxIndex;
        public int? InboxIndex
        {
            get { return _inboxIndex; }
            set
            {
                _inboxIndex = value;
                CallPropertyChanged(nameof(InboxIndex));
            }
        }

        private int? _projectIndex;
        public int? ProjectIndex
        {
            get { return _projectIndex; }
            set
            {
                _projectIndex = value;
                CallPropertyChanged(nameof(ProjectIndex));
            }
        }

        private int? _contextIndex;
        public int? ContextIndex
        {
            get { return _contextIndex; }
            set
            {
                _contextIndex = value;
                CallPropertyChanged(nameof(ContextIndex));
            }
        }

        private int? _todayIndex;
        public int? TodayIndex
        {
            get { return _todayIndex; }
            set
            {
                _todayIndex = value;
                CallPropertyChanged(nameof(TodayIndex));
            }
        }
    }
}
