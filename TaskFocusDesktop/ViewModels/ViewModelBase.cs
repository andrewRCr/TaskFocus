using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskFocusDesktop.ViewModels
{
    public abstract class ViewModelBase : Screen
    {
        protected enum ViewModelChildren
        {
            InboxVM,
            TodayVM,
            ProjectsVM,
            ContextsVM,
            LoginVM
        }

        protected ViewModelChildren ActiveViewModel;
    }
}
