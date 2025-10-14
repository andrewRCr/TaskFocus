using System;
using System.Collections.Generic;
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
using TaskFocusDesktop.Views.Components;

namespace TaskFocusDesktop.Views.MainContent
{
    /// <summary>
    /// Interaction logic for ContextsView.xaml
    /// </summary>
    public partial class ContextsView : UserControl
    {
        public ContextsView()
        {
            InitializeComponent();
        }

        private void MainGridBackground_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            List<Type> typesToIgnore = new()
            {
                typeof(TaskItemContainerView),
            };

            if (typesToIgnore.Contains(e.Source.GetType())) { return; }

            this.MainGridBackground.Focus();
        }
    }
}
