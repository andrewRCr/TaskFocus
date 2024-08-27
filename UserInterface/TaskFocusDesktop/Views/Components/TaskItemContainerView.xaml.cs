using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TaskFocusDesktop.Commands;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library.Models;

namespace TaskFocusDesktop.Views.Components
{
    /// <summary>
    /// Interaction logic for TaskItemContainerView.xaml
    /// </summary>
    public partial class TaskItemContainerView : UserControl
    {
        //public static readonly DependencyProperty ContainerTasksProperty =
        //    DependencyProperty.Register("ContainerTasks", typeof(ObservableCollection<TaskDisplayModel>), typeof(TaskItemContainerView), new PropertyMetadata(null));

        //public ObservableCollection<TaskDisplayModel> ContainerTasks
        //{
        //    get { return (ObservableCollection<TaskDisplayModel>)GetValue(ContainerTasksProperty); }
        //    set { SetValue(ContainerTasksProperty, value); }
        //}

        private bool _isPreviousLocalOrderStored = false;
        private Dictionary<TaskDisplayModel, int> _previousLocalOrder;

        private object? _lastSelection;

        public static readonly DependencyProperty IsDraggingProperty =
            DependencyProperty.Register("IsDragging", typeof(bool), typeof(TaskItemContainerView), new PropertyMetadata(false));

        public bool IsDragging
        {
            get { return (bool)GetValue(IsDraggingProperty); }
            set { SetValue(IsDraggingProperty, value); }
        }

        public static readonly DependencyProperty CanReorderProperty =
            DependencyProperty.Register("CanReorder", typeof(bool), typeof(TaskItemContainerView), new PropertyMetadata(false));

        public bool CanReorder
        {
            get { return (bool)GetValue(CanReorderProperty); }
            set { SetValue(CanReorderProperty, value); }
        }

        public static readonly DependencyProperty OrderingIndexProperty =
            DependencyProperty.Register("OrderingIndex", typeof(string), typeof(TaskItemContainerView), new PropertyMetadata(null));

        public string OrderingIndex
        {
            get { return (string)GetValue(OrderingIndexProperty); }
            set { SetValue(OrderingIndexProperty, value); }
        }

        public static readonly DependencyProperty TaskItemInsertedCommandProperty =
            DependencyProperty.Register("TaskItemInsertedCommand", typeof(RelayCommand), typeof(TaskItemContainerView), new PropertyMetadata(null));

        public RelayCommand TaskItemInsertedCommand
        {
            get { return (RelayCommand)GetValue(TaskItemInsertedCommandProperty); }
            set { SetValue(TaskItemInsertedCommandProperty, value); }
        }

        public TaskItemContainerView()
        {
            InitializeComponent();

            _previousLocalOrder = new Dictionary<TaskDisplayModel, int>();
        }

        private void TaskItem_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && sender is FrameworkElement frameworkElement && CanReorder)
            {
                List<Type> typesToIgnore = new() { 
                    typeof(CheckBox), typeof(TextBox), typeof(ComboBox), typeof(PackIcon), typeof(DatePicker), typeof(Button), 
                    typeof(CustomControls.CircularButton), typeof(CustomControls.CheckBoxButton) };
                string textBoxViewStr = "System.Windows.Controls.TextBoxView"; // internal WPF component; no public API

                // ignore drag if clicked on any controls
                if (typesToIgnore.Contains(e.OriginalSource.GetType()) || e.OriginalSource.ToString() == textBoxViewStr)
                {
                    e.Handled = true;
                    return;
                }

                // flag for highlighting
                IsDragging = true;
                //Debug.WriteLine("MOUSE DOWN - DRAGGING");

                object taskItem = frameworkElement.DataContext;
                DragDropEffects dragDropResult =  DragDrop.DoDragDrop(frameworkElement, 
                    new DataObject(DataFormats.Serializable, taskItem), DragDropEffects.Move);

                if (dragDropResult == DragDropEffects.None)
                {
                    UndoPreviewInsertTaskItem();
                    IsDragging = false;
                    //Debug.WriteLine("MOUSE UP - DRAGGING STOPPED");
                }
            }
        }

        private void TaskItem_DragEnter(object sender, DragEventArgs e)
        {
            if (sender is FrameworkElement frameworkElement && CanReorder)
            {
                var targetTaskItem = (TaskDisplayModel)frameworkElement.DataContext; // task dropped onto
                var insertedTaskItem = (TaskDisplayModel)e.Data.GetData(DataFormats.Serializable); // dropped task

                if (!_isPreviousLocalOrderStored)
                {
                    _previousLocalOrder.Clear();

                    var vm = (TaskViewModelBase)TaskContainerListBox.DataContext;
                    foreach (var task in vm.LocalTasks!) { _previousLocalOrder.Add(task, vm.LocalTasks!.IndexOf(task)); }
                    _isPreviousLocalOrderStored = true;
                }

                PreviewInsertTaskItem(insertedTaskItem, targetTaskItem);
            }
        }

        private void TaskItem_DragLeave(object sender, DragEventArgs e)
        {
            HitTestResult result = VisualTreeHelper.HitTest(TaskContainerListBox, e.GetPosition(TaskContainerListBox));

            if (result == null) 
            { 
                // if dragged out of container entirely, revert local order
                UndoPreviewInsertTaskItem();
            }
        }

        private void TaskItem_Drop(object sender, DragEventArgs e)
        {
            if (sender is FrameworkElement frameworkElement && CanReorder)
            {
                // remove visual highlighting flag
                IsDragging = false;
                //Debug.WriteLine("DRAGGING STOPPED");

                // drag/drop action was fully completed; update remote order accordingly
                var vm = (TaskViewModelBase)TaskContainerListBox.DataContext;
                var insertedTaskItem = (TaskDisplayModel)e.Data.GetData(DataFormats.Serializable);
                int previousIndex = _previousLocalOrder[insertedTaskItem];

                bool orderChanged = previousIndex != vm.LocalTasks!.IndexOf(insertedTaskItem);
                if (orderChanged) { UpdateRemoteOrder(); }
            }
        }

        public void PreviewInsertTaskItem(TaskDisplayModel insertedTaskItem, TaskDisplayModel targetTaskItem)
        {
            if (insertedTaskItem == targetTaskItem) { return; }

            var vm = (TaskViewModelBase)TaskContainerListBox.DataContext;
            int oldIndex = vm.LocalTasks!.IndexOf(insertedTaskItem);
            int nextIndex = vm.LocalTasks!.IndexOf(targetTaskItem);

            if (oldIndex != -1 && nextIndex != -1)
            {
                // update local order
                vm.LocalTasks.Move(oldIndex, nextIndex);
            }
        }

        public void UndoPreviewInsertTaskItem()
        {
            var vm = (TaskViewModelBase)TaskContainerListBox.DataContext;

            foreach (var taskIndexPair in _previousLocalOrder)
            {
                vm.LocalTasks!.Move(vm.LocalTasks.IndexOf(taskIndexPair.Key), _previousLocalOrder[taskIndexPair.Key]);
            }

            _previousLocalOrder.Clear();
            _isPreviousLocalOrderStored = false;
        }

        private void UpdateRemoteOrder()
        {
            var vm = (TaskViewModelBase)TaskContainerListBox.DataContext;
            vm.CanUpdateOrderingIndices = false;

            foreach (var item in vm.LocalTasks!)
            {
                if (vm.LocalTasks.IndexOf(item) == vm.LocalTasks.Count - 1)
                {
                    // on last one; can update all collection indices now
                    vm.CanUpdateOrderingIndices = true;
                }

                // will trigger a DataService.UpdateCollectionOrderingIndices call
                item[OrderingIndex] = vm.LocalTasks.IndexOf(item);
                //Debug.WriteLine($"{item.TaskName} OrderingIndex({OrderingIndex}): {item[OrderingIndex]}");
            }
        }

        // when the observable collection LocalTasks is reordered via its Move method (in PreviewInsertTaskItem),
        // moved items are temporarily removed before being re-inserted. when these are also the selected item, WPF
        // will "prematurely" set the SelectedItem to null and selection will not be preserved once the item is
        // re-added at its new index. this workaround preserves that selection.
        private void TaskContainerListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems != null && (e.AddedItems == null || e.AddedItems.Count == 0))
            {
                _lastSelection = e.RemovedItems.OfType<object>().FirstOrDefault()!;
                TaskContainerListBox.SelectedItem = _lastSelection;
            }
        }
    }
}
