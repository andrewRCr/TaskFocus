using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace TaskFocusUI.Library.Models
{
    public class UserSettingsDisplayModel : INotifyPropertyChanged
    {
        public string Id { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public void CallPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // user-editable properties
        // ====================
        private bool _cleanUpImmediately = false;
        public bool CleanUpImmediately
        {
            get { return _cleanUpImmediately; }
            set
            {
                _cleanUpImmediately = value;
                CallPropertyChanged(nameof(CleanUpImmediately));
            }
        }

        private int _cleanUpDelayDays = 7;
        public int CleanUpDelayDays
        {
            get { return _cleanUpDelayDays; }
            set
            {
                _cleanUpDelayDays = value;
                CallPropertyChanged(nameof(CleanUpDelayDays));
            }
        }

        private int _deleteDelayDays = 30;
        public int DeleteDelayDays
        {
            get { return _deleteDelayDays; }
            set
            {
                _deleteDelayDays = value;
                CallPropertyChanged(nameof(DeleteDelayDays));
            }
        }
    }
}
