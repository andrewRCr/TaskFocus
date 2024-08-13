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

namespace TaskFocusDesktop.Views.MainContent
{
    /// <summary>
    /// Interaction logic for CompletedView.xaml
    /// </summary>
    public partial class CompletedView : UserControl
    {
        public CompletedView()
        {
            InitializeComponent();
        }

        private void MainGridBackground_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            this.MainGridBackground.Focus();
        }
    }
}
