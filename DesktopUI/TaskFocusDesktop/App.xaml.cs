using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace TaskFocusDesktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // define application exception handler
            Application.Current.DispatcherUnhandledException +=
                AppDispatcherUnhandledException;

            // defer other startup processing to base class
            base.OnStartup(e);
        }


        private void AppDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            runException(e.Exception);

            e.Handled = true;
        }

        void runException(Exception ex)
        {
            MessageBox.Show(
                String.Format(
                    "{0} Error:  {1}\r\n\r\n{2}",
                    ex.Source, ex.Message, ex.StackTrace,
                    "Initialize Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error));

            if (ex.InnerException != null)
            {
                runException(ex.InnerException);
            }
        }
    }
}
