using System;
using System.Collections;
using System.Windows;
using System.Windows.Data;
using TaskFocusUI.Library.Models;

namespace TaskFocusDesktop.Utilities
{
    public class DictionaryItemBooleanToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values != null)
            {
                var myDict = values[0] as IDictionary;
                var myKey = values[1] as ProjectDisplayModel;
                if (myDict != null && myKey != null)
                {
                    if (myDict[myKey] != null)
                    {
                        return (bool)myDict[myKey]! == true ? Visibility.Visible : Visibility.Hidden;
                    }                  
                }
            }
            return Binding.DoNothing;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
