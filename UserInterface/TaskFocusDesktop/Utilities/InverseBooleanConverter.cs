using Nextended.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace TaskFocusDesktop.Utilities
{
    class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, 
                              object parameter, CultureInfo culture)
        {
            var boolValue = (bool)value;
            if (!boolValue.IsNull()) { return !boolValue; }

            throw new InvalidOperationException("The target must be a boolean!");
        }

        public object ConvertBack(object value, Type targetType, 
            object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
