using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Reflection;

namespace TaskFocusUI.Library.Models
{
    public class ContextDisplayModel : INotifyPropertyChanged, ICollectionDisplayModel, ISyncableData
    {
        public int? Id { get; set; }
        public string UserId { get; set; }

        // for sync
        public ESyncableDataType DataType { get; } = ESyncableDataType.Context;
        public DateTimeOffset ServerLastUpdated { get; set; }
        public DateTimeOffset ClientLastUpdated { get; set; }
        public DateTimeOffset? Deleted { get; set; }
        // for local tracking of new adds pre-push
        public int? TempLocalId { get; set; }

        // user-editable properties
        // ====================
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
                var properties = typeof(ContextDisplayModel)
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
                Type thisType = typeof(ContextDisplayModel);
                PropertyInfo? thisPropInfo = thisType.GetProperty(propertyName);
                if (thisPropInfo != null) thisPropInfo.SetValue(this, value, null);
            }
        }

        // for deep copies
        public ContextDisplayModel Clone()
        {
            var serialized = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<ContextDisplayModel>(serialized)!;
        }

        // for updating while maintaining references
        public void ValueAssign(ContextDisplayModel source)
        {
            Id = source.Id;
            UserId = source.UserId;
            ContextName = source.ContextName;
            OrderIndex = source.OrderIndex;
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
