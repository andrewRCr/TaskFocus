using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TaskFocusDesktop.Views.Components;

namespace TaskFocusDesktop.Views.MainContent
{
    /// <summary>
    /// Interaction logic for InboxView.xaml
    /// </summary>
    public partial class InboxView : UserControl
    {
        public InboxView()
        {
            InitializeComponent();
        }

        private void MainGridBackground_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            //Debug.WriteLine($"SourceType = {e.Source.GetType()}");

            List<Type> typesToIgnore = new()
            {
                typeof(TaskItemContainerView),
            };

            if (typesToIgnore.Contains(e.Source.GetType())) { return; }

            this.MainGridBackground.Focus();
        }
    }
}
