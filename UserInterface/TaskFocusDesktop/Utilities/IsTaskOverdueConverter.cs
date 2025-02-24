using System;
using System.Globalization;
using System.Windows.Data;

namespace TaskFocusDesktop.Utilities
{
    class IsTaskOverdueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime? dueDateValue = (DateTime)value;
            if (dueDateValue != null) { return dueDateValue < DateTime.Now; }

            throw new InvalidOperationException("The target must be a DateTime!");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
