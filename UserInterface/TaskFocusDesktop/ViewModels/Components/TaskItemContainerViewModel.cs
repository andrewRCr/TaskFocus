using System.Windows.Controls;

namespace TaskFocusDesktop.ViewModels.Components
{
    public class TaskItemContainerViewModel : UserControl
    {
        // to be bound on relevant TaskView to its specific TaskViewModel properties when TaskItemContainerView controls are instantiated
        public bool CanReorder => false;
        public string OrderingIndex => string.Empty;
        public int ListBoxHeight => 0;

        public TaskItemContainerViewModel()
        {
        }
    }
}
