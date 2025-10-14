using System.Windows;
using System.Windows.Controls;
using TaskFocusDesktop.Commands;

namespace TaskFocusDesktop.CustomControls
{
    class TaskCollectionComboBox : ComboBox
    {
        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.Register("PlaceholderText", typeof(string), typeof(TaskCollectionComboBox), new PropertyMetadata(string.Empty));

        public string PlaceholderText
        {
            get { return (string)GetValue(PlaceholderTextProperty); }
            set { SetValue(PlaceholderTextProperty, value); }
        }

        public static readonly DependencyProperty ClearCollectionCommandProperty =
            DependencyProperty.Register("ClearCollectionCommand", typeof(RelayCommand), typeof(TaskCollectionComboBox), new PropertyMetadata(null));

        public RelayCommand ClearCollectionCommand
        {
            get { return (RelayCommand)GetValue(ClearCollectionCommandProperty); }
            set { SetValue(ClearCollectionCommandProperty, value); }
        }
    }
}
