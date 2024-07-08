using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using TaskFocusDesktop.Commands;
using TaskFocusUI.Library.Models;

namespace TaskFocusDesktop.ViewModels.Components
{
    public class TaskItemContainerViewModel : UserControl
    {
        // to be bound on relevant TaskView to its specific TaskViewModel properties when TaskItemContainerView controls are instantiated
        public bool CanReorder => false;
        public string OrderingIndex => string.Empty;

        public TaskItemContainerViewModel()
        { 
        }
    }
}
