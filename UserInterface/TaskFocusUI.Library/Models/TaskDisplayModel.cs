using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;

namespace TaskFocusUI.Library.Models
{
    public class TaskDisplayModel : INotifyPropertyChanged, ISyncableData
    {
        public int? Id { get; set; }
        public string UserId { get; set; }
        public DateTime? DateCompleted { get; set; }
        public int? ProjectId { get; set; }
        public int? ContextId { get; set; }

        // for sync
        public ESyncableDataType DataType { get; } = ESyncableDataType.Task;
        public DateTimeOffset ServerLastUpdated { get; set; }
        public DateTimeOffset ClientLastUpdated { get; set; }
        public DateTimeOffset? Deleted { get; set; }
        // for local tracking of new adds pre-push
        public int? TempLocalId { get; set; }

        // directly editable (by user or app) properties
        // ====================
        private string? _taskName;
        public string? TaskName
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

        private string? _projectName;
        public string? ProjectName
        {
            get { return _projectName; }
            set
            {
                _projectName = value;
                CallPropertyChanged(nameof(ProjectName));
            }
        }

        private string? _contextName;
        public string? ContextName
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

        // helper properties / methods
        // ====================

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
                        return property.GetValue(this, null)!;
                }

                throw new ArgumentException($"Can't find property {propertyName}!");

            }
            set
            {
                Type thisType = typeof(TaskDisplayModel);
                PropertyInfo? thisPropInfo = thisType.GetProperty(propertyName);
                if (thisPropInfo != null) thisPropInfo.SetValue(this, value, null);
            }
        }

        // for deep copies
        public TaskDisplayModel Clone()
        {
            var serialized = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<TaskDisplayModel>(serialized)!;
        }

        // for updating while maintaining references
        public void ValueAssign(TaskDisplayModel source)
        {
            Id = source.Id;
            UserId = source.UserId;
            TaskName = source.TaskName;
            ProjectId = source.ProjectId;
            ProjectName = source.ProjectName;
            ContextId = source.ContextId;
            ContextName = source.ContextName;
            DueDate = source.DueDate;
            Starred = source.Starred;
            Completed = source.Completed;
            DateCompleted = source.DateCompleted;
            CleanedUp = source.CleanedUp;
            InboxIndex = source.InboxIndex;
            TodayIndex = source.TodayIndex;
            ProjectIndex = source.ProjectIndex;
            ContextIndex = source.ContextIndex;
            ServerLastUpdated = source.ServerLastUpdated;
            ClientLastUpdated = source.ClientLastUpdated;
            Deleted = source.Deleted;
            TempLocalId = source.TempLocalId;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void CallPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
