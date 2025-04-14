using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Reflection;

namespace TaskFocusUI.Library.Models
{
    public class ProjectDisplayModel : INotifyPropertyChanged, ICollectionDisplayModel, ISyncableData
    {
        public int? Id { get; set; }
        public string UserId { get; set; }
        public int? ContextId { get; set; }
        public string ContextName { get; set; }
        public DateTime? DueDate { get; set; }
        public bool Completed { get; set; } = false;
        public DateTime? DateCompleted { get; set; }

        // for sync
        public ESyncableDataType DataType { get; } = ESyncableDataType.Project;
        public DateTimeOffset ServerLastUpdated { get; set; }
        public DateTimeOffset ClientLastUpdated { get; set; }
        public DateTimeOffset? Deleted { get; set; }
        // for local tracking of new adds pre-push
        public int? TempLocalId { get; set; }

        // user-editable properties
        // ====================
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

        private int? _orderIndex;
        public int? OrderIndex
        {
            get { return _orderIndex; }
            set
            {
                _orderIndex = value;
                CallPropertyChanged(nameof(OrderIndex));
            }
        }

        // indexer
        public object this[string propertyName]
        {
            get
            {
                var properties = typeof(ProjectDisplayModel)
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
                Type thisType = typeof(ProjectDisplayModel);
                PropertyInfo? thisPropInfo = thisType.GetProperty(propertyName);
                if (thisPropInfo != null) thisPropInfo.SetValue(this, value, null);
            }
        }

        public ProjectDisplayModel Clone()
        {
            var serialized = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<ProjectDisplayModel>(serialized)!;
        }

        // for updating while maintaining references
        public void ValueAssign(ProjectDisplayModel source)
        {
            Id = source.Id;
            UserId = source.UserId;
            ProjectName = source.ProjectName;
            OrderIndex = source.OrderIndex;
            ContextId = source.ContextId;
            ContextName = source.ContextName;
            DueDate = source.DueDate;
            Completed = source.Completed;
            DateCompleted = source.DateCompleted;
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
