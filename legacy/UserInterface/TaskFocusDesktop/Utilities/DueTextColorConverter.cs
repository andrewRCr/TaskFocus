using System.Globalization;
using System;
using System.Windows.Data;

namespace TaskFocusDesktop.Utilities
{
    class DueTextColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                DateTime? nullableDueDateValue = (DateTime)value;
                if (nullableDueDateValue != null)
                {
                    DateTime dueDateValue = (DateTime)value;
                    if (dueDateValue == DateTime.Today) { return "Green"; }
                    else if (dueDateValue < DateTime.Today) { return "#f64e62"; }
                    else return "#c2c2c5";  // foreground main
                }
            }
            return "c2c2c5"; // default: foreground main
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
