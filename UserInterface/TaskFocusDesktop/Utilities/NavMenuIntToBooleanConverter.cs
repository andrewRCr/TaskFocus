using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace TaskFocusDesktop.Utilities
{
    class NavMenuIntToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var paramVal = parameter as string;
            var objVal = ((int)value).ToString();

            return paramVal == objVal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
            {
                var i = System.Convert.ToInt32((parameter ?? "0") as string);

                return ((bool)value)
                    ? System.Convert.ChangeType(i, targetType)
                    : 0;
            }

            return 0; // returning a zero provides a case where none of the menu items appear checked
        }
    }
}
