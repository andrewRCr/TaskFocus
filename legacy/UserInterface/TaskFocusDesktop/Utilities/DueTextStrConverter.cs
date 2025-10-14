using System;
using System.Globalization;
using System.Windows.Data;

namespace TaskFocusDesktop.Utilities
{
    class DueTextStrConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                DateTime? nullableDueDateValue = (DateTime)value;
                if (nullableDueDateValue != null)
                {
                    DateTime dueDateValue = (DateTime)value;
                    if (dueDateValue == DateTime.Today) { return "Today"; }
                    else if (dueDateValue == DateTime.Today.AddDays(1)) { return "Tomorrow"; }
                    else if (dueDateValue == DateTime.Today.AddDays(-1)) { return "Yesterday"; }
                    else { return dueDateValue.ToShortDateString()!; }
                }
            }
            
            return string.Empty; // default
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
