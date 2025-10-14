using System;
using System.Globalization;
using System.Windows.Data;

namespace TaskFocusDesktop.Utilities
{
    class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, 
                              object parameter, CultureInfo culture)
        {
            Boolean? boolValue = (bool)value;
            if (boolValue != null) 
            { return !boolValue; }

            throw new InvalidOperationException("The target must be a boolean!");
        }

        public object ConvertBack(object value, Type targetType, 
            object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
