using Nextended.Core.Extensions;
using System;
using System.Globalization;
using System.Windows.Data;

namespace TaskFocusDesktop.Utilities
{
    class IsNullConverter : IValueConverter
    {
        enum Parameters
        {
            Normal, IsNullOrWhiteSpace
        }

        public object Convert(object value, Type targetType, 
                              object parameter, CultureInfo culture)
        {
            if (parameter != null)
            {
                var direction = (Parameters)Enum.Parse(typeof(Parameters), (string)parameter);
                if (direction == Parameters.IsNullOrWhiteSpace)
                {
                    string valueStr = (string)value;
                    return valueStr.IsNullOrWhiteSpace();
                }
            }

            return value == null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            return Binding.DoNothing;
        }
    }
}
