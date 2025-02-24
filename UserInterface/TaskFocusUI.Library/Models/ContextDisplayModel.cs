using System;
using System.ComponentModel;
using System.Reflection;

namespace TaskFocusUI.Library.Models
{
    public class ContextDisplayModel : INotifyPropertyChanged, ICollectionDisplayModel
    {
        public int? Id { get; set; }
        public string UserId { get; set; }

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
                        return property.GetValue(this, null);
                }

                throw new ArgumentException($"Can't find property {propertyName}!");

            }
            set
            {
                Type myType = typeof(ContextDisplayModel);
                PropertyInfo myPropInfo = myType.GetProperty(propertyName);
                myPropInfo.SetValue(this, value, null);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void CallPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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
    }
}
