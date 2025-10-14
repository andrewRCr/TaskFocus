using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TaskFocusUI.Library.Models;

namespace TaskFocusDesktop.Views.Components
{
    /// <summary>
    /// Interaction logic for NewTaskItemView.xaml
    /// </summary>
    public partial class NewTaskItemView : UserControl
    {
        public NewTaskItemView()
        {
            InitializeComponent();
        }

        private void CollectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DependencyObject dependencyObject)
            {
                this.MainTaskItemGrid.Focus();
            }
        }

        private void CollectionComboBox_DropDownClosed(object sender, EventArgs e)
        {
            if (sender is DependencyObject dependencyObject)
            {
                this.MainTaskItemGrid.Focus();
            }
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DependencyObject dependencyObject)
            {
                this.MainTaskItemGrid.Focus();
            }
        }
    }
}
