using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TaskFocusDesktop.Utilities
{
    class ActiveMainViewToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            string sendingMenuItemStr = values[0].ToString();
            Enum activeMainContentViewEnum = (Enum)values[1];
            string activeMainContentViewEnumStr = activeMainContentViewEnum.ToString();

            if (sendingMenuItemStr == "[ Completed ]") { sendingMenuItemStr = "Completed"; }

            return sendingMenuItemStr == activeMainContentViewEnumStr ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
