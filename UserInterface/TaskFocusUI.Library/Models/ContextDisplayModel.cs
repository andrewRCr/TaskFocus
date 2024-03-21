using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace TaskFocusUI.Library.Models
{
    public class ContextDisplayModel : INotifyPropertyChanged
    {
        public int? Id { get; set; }
        public string UserId { get; set; }

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
    }
}
