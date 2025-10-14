using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TaskFocusDesktop.Utilities
{
    class IsSelectedCollectionToColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            string sendingMenuItemCollectionName = values[0].ToString();
            string currentlySelectedCollectionName = values[1].ToString();
            if (currentlySelectedCollectionName == null) return (SolidColorBrush)new BrushConverter().ConvertFrom("#c2c2c5")!;

            bool isSelectedCollection = sendingMenuItemCollectionName == currentlySelectedCollectionName;
            string hexValue = isSelectedCollection ? "#776be7" : "#c2c2c5"; // foreground highlight/main
            return (SolidColorBrush)new BrushConverter().ConvertFrom(hexValue)!;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
