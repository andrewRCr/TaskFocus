using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using TaskFocusDesktop.Commands;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusDesktop.ViewModels.MainContent;
using TaskFocusUI.Library.Models;

namespace TaskFocusDesktop.Views.Components
{
    /// <summary>
    /// Interaction logic for TaskItemContainerView.xaml
    /// </summary>
    public partial class TaskItemContainerView : UserControl
    {
        public static readonly DependencyProperty ContainerTasksProperty =
            DependencyProperty.Register("ContainerTasks", typeof(ObservableCollection<TaskDisplayModel>), typeof(TaskItemContainerView), new PropertyMetadata(null));

        public ObservableCollection<TaskDisplayModel> ContainerTasks
        {
            get { return (ObservableCollection<TaskDisplayModel>)GetValue(ContainerTasksProperty); }
            set { SetValue(ContainerTasksProperty, value); }
        }

        public static readonly DependencyProperty IsProjectContainerProperty =
                DependencyProperty.Register("IsProjectContainer", typeof(bool), typeof(TaskItemContainerView), new PropertyMetadata(false));
        
        public bool IsProjectContainer
        {
            get { return (bool)GetValue(IsProjectContainerProperty); }
            set { SetValue(IsProjectContainerProperty, value); }
        }

        public static readonly DependencyProperty IsContextContainerProperty =
        DependencyProperty.Register("IsContextContainer", typeof(bool), typeof(TaskItemContainerView), new PropertyMetadata(false));

        public bool IsContextContainer
        {
            get { return (bool)GetValue(IsContextContainerProperty); }
            set { SetValue(IsContextContainerProperty, value); }
        }

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

        public int ListBoxHeight
        {
            get { return (int)GetValue(ListBoxHeightProperty); }
            set { SetValue(ListBoxHeightProperty, value); }
        }

        public static readonly DependencyProperty ListBoxHeightProperty =
            DependencyProperty.Register("ListBoxHeight", typeof(int), typeof(TaskItemContainerView), new PropertyMetadata(null));

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
                    typeof(CheckBox), typeof(TextBox), typeof(TextBlock), typeof(PackIcon), typeof(DatePicker), typeof(ComboBox), 
                    typeof(Button), typeof(CustomControls.CircularButton), typeof(CalendarDayButton),
                    typeof(CustomControls.CheckBoxButton), typeof(CustomControls.TaskCollectionComboBox), typeof(DatePickerTextBox) };
                string textBoxViewStr = "System.Windows.Controls.TextBoxView"; // internal WPF component; no public API

                // ignore drag if clicked on any controls
                if (typesToIgnore.Contains(e.OriginalSource.GetType()) || e.OriginalSource.ToString() == textBoxViewStr)
                {
                    e.Handled = true;
                    //Debug.WriteLine($"OriginalSourceType = {e.OriginalSource.GetType()}; IGNORED");
                    return;
                }

                // flag for highlighting
                IsDragging = true;
                //Debug.WriteLine("MOUSE DOWN - DRAGGING");

                object taskItem = frameworkElement.DataContext;
                DragDropEffects dragDropResult = DragDrop.DoDragDrop(frameworkElement,
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

                    if (IsProjectContainer)
                    {
                        var vm = (ProjectsViewModel)TaskContainerListBox.DataContext;
                        foreach (var task in vm.FocusedProjectTasks!) { _previousLocalOrder.Add(task, vm.FocusedProjectTasks!.IndexOf(task)); }
                    }
                    else if (IsContextContainer)
                    {
                        var vm = (ContextsViewModel)TaskContainerListBox.DataContext;
                        foreach (var task in vm.FocusedContextTasks!) { _previousLocalOrder.Add(task, vm.FocusedContextTasks!.IndexOf(task)); }
                    }
                    else
                    {
                        var vm = (TaskViewModelBase)TaskContainerListBox.DataContext;
                        foreach (var task in vm.LocalTasks!) { _previousLocalOrder.Add(task, vm.LocalTasks!.IndexOf(task)); }
                    }

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
                if (IsProjectContainer)
                {
                    var vm = (ProjectsViewModel)TaskContainerListBox.DataContext;
                    var insertedTaskItem = (TaskDisplayModel)e.Data.GetData(DataFormats.Serializable);
                    int previousIndex = _previousLocalOrder[insertedTaskItem];

                    bool orderChanged = previousIndex != vm.FocusedProjectTasks!.IndexOf(insertedTaskItem);
                    if (orderChanged) { UpdateDataStateOrder(); }
                }
                else if (IsContextContainer)
                {
                    var vm = (ContextsViewModel)TaskContainerListBox.DataContext;
                    var insertedTaskItem = (TaskDisplayModel)e.Data.GetData(DataFormats.Serializable);
                    int previousIndex = _previousLocalOrder[insertedTaskItem];

                    bool orderChanged = previousIndex != vm.FocusedContextTasks!.IndexOf(insertedTaskItem);
                    if (orderChanged) { UpdateDataStateOrder(); }
                }
                else
                {
                    var vm = (TaskViewModelBase)TaskContainerListBox.DataContext;
                    var insertedTaskItem = (TaskDisplayModel)e.Data.GetData(DataFormats.Serializable);
                    int previousIndex = _previousLocalOrder[insertedTaskItem];

                    bool orderChanged = previousIndex != vm.LocalTasks!.IndexOf(insertedTaskItem);
                    if (orderChanged) { UpdateDataStateOrder(); }
                }
            }
        }

        public void PreviewInsertTaskItem(TaskDisplayModel insertedTaskItem, TaskDisplayModel targetTaskItem)
        {
            if (insertedTaskItem == targetTaskItem) return;

            if (IsProjectContainer)
            {
                var vm = (ProjectsViewModel)TaskContainerListBox.DataContext;
                int oldIndex = vm.FocusedProjectTasks!.IndexOf(insertedTaskItem);
                int nextIndex = vm.FocusedProjectTasks!.IndexOf(targetTaskItem);

                if (oldIndex != -1 && nextIndex != -1)
                {
                    // update local order
                    vm.FocusedProjectTasks.Move(oldIndex, nextIndex);
                }
            }
            else if (IsContextContainer)
            {
                var vm = (ContextsViewModel)TaskContainerListBox.DataContext;
                int oldIndex = vm.FocusedContextTasks!.IndexOf(insertedTaskItem);
                int nextIndex = vm.FocusedContextTasks!.IndexOf(targetTaskItem);

                if (oldIndex != -1 && nextIndex != -1)
                {
                    // update local order
                    vm.FocusedContextTasks.Move(oldIndex, nextIndex);
                }
            }
            else
            {
                var vm = (TaskViewModelBase)TaskContainerListBox.DataContext;
                int oldIndex = vm.LocalTasks!.IndexOf(insertedTaskItem);
                int nextIndex = vm.LocalTasks!.IndexOf(targetTaskItem);

                if (oldIndex != -1 && nextIndex != -1)
                {
                    // update local order
                    vm.LocalTasks.Move(oldIndex, nextIndex);
                }
            }
        }

        public void UndoPreviewInsertTaskItem()
        {
            if (IsProjectContainer)
            {
                var vm = (ProjectsViewModel)TaskContainerListBox.DataContext;
                foreach (var taskIndexPair in _previousLocalOrder)
                {
                    vm.FocusedProjectTasks!.Move(vm.FocusedProjectTasks.IndexOf(taskIndexPair.Key), _previousLocalOrder[taskIndexPair.Key]);
                }
            }
            else if (IsContextContainer)
            {
                var vm = (ContextsViewModel)TaskContainerListBox.DataContext;
                foreach (var taskIndexPair in _previousLocalOrder)
                {
                    vm.FocusedContextTasks!.Move(vm.FocusedContextTasks.IndexOf(taskIndexPair.Key), _previousLocalOrder[taskIndexPair.Key]);
                }
            }
            else
            {
                var vm = (TaskViewModelBase)TaskContainerListBox.DataContext;

                foreach (var taskIndexPair in _previousLocalOrder)
                {
                    vm.LocalTasks!.Move(vm.LocalTasks.IndexOf(taskIndexPair.Key), _previousLocalOrder[taskIndexPair.Key]);
                }
            }

            _previousLocalOrder.Clear();
            _isPreviousLocalOrderStored = false;
        }

        private void UpdateDataStateOrder()
        {
            if (IsProjectContainer)
            {
                var vm = (ProjectsViewModel)TaskContainerListBox.DataContext;
                vm.CanUpdateOrderingIndices = false;

                foreach (var item in vm.FocusedProjectTasks!)
                {
                    if (vm.FocusedProjectTasks.IndexOf(item) == vm.FocusedProjectTasks.Count - 1)
                    {
                        // on last one; can update all collection indices now
                        vm.CanUpdateOrderingIndices = true;
                    }

                    // will trigger a PropertyChanged -> UpdateTaskData call
                    bool changed = (int)item[OrderingIndex] != vm.FocusedProjectTasks.IndexOf(item);
                    if (changed) item[OrderingIndex] = vm.FocusedProjectTasks.IndexOf(item);
                    //Debug.WriteLine($"{item.TaskName} OrderingIndex({OrderingIndex}): {item[OrderingIndex]}");

                    // ensure rebuilt local on next drag/drop
                    _previousLocalOrder.Clear();
                    _isPreviousLocalOrderStored = false;
                }
            }
            else if (IsContextContainer)
            {
                var vm = (ContextsViewModel)TaskContainerListBox.DataContext;
                vm.CanUpdateOrderingIndices = false;

                foreach (var item in vm.FocusedContextTasks!)
                {
                    if (vm.FocusedContextTasks.IndexOf(item) == vm.FocusedContextTasks.Count - 1)
                    {
                        // on last one; can update all collection indices now
                        vm.CanUpdateOrderingIndices = true;
                    }

                    // will trigger a PropertyChanged -> UpdateTaskData call
                    bool changed = (int)item[OrderingIndex] != vm.FocusedContextTasks.IndexOf(item);
                    if (changed) item[OrderingIndex] = vm.FocusedContextTasks.IndexOf(item);
                    //Debug.WriteLine($"{item.TaskName} OrderingIndex({OrderingIndex}): {item[OrderingIndex]}");
                }
            }
            else
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

                    // will trigger a PropertyChanged -> UpdateTaskData call
                    bool changed = (int)item[OrderingIndex] != vm.LocalTasks.IndexOf(item);
                    if (changed) item[OrderingIndex] = vm.LocalTasks.IndexOf(item);
                    //Debug.WriteLine($"{item.TaskName} OrderingIndex({OrderingIndex}): {item[OrderingIndex]}");
                }
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
