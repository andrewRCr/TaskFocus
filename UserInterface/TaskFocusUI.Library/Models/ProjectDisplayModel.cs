using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace TaskFocusUI.Library.Models
{
    public class ProjectDisplayModel : INotifyPropertyChanged, ICollectionDisplayModel
    {
        public int? Id { get; set; }
        public string UserId { get; set; }
        public int? ContextId { get; set; }
        public string ContextName { get; set; }
        public DateTime? DueDate { get; set; }
        public bool Completed { get; set; } = false;
        public DateTime? DateCompleted { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public void CallPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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
    }
}
