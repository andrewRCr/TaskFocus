using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private bool _isPreviousLocalOrderStored = false;
        private Dictionary<TaskDisplayModel, int> _previousLocalOrder;

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
            if (e.LeftButton == MouseButtonState.Pressed && sender is FrameworkElement frameworkElement)
            {
                List<Type> typesToIgnore = new() { 
                    typeof(CheckBox), typeof(ComboBox), typeof(PackIcon), typeof(DatePicker), typeof(Button) };
                string textBoxViewStr = "System.Windows.Controls.TextBoxView"; // internal WPF component; no public API

                // ignore drag if clicked on any controls
                if (typesToIgnore.Contains(e.OriginalSource.GetType()) || e.OriginalSource.ToString() == textBoxViewStr)
                {
                    e.Handled = true;
                    return;
                }

                object taskItem = frameworkElement.DataContext;
                DragDropEffects dragDropResult =  DragDrop.DoDragDrop(frameworkElement, 
                    new DataObject(DataFormats.Serializable, taskItem), DragDropEffects.Move);

                if (dragDropResult == DragDropEffects.None)
                {
                    UndoPreviewInsertTaskItem();
                }
            }
        }

        private void TaskItem_DragEnter(object sender, DragEventArgs e)
        {
            if (sender is FrameworkElement frameworkElement && CanReorder)
            {
                var targetTaskItem = (TaskDisplayModel)frameworkElement.DataContext;
                var insertedTaskItem = (TaskDisplayModel)e.Data.GetData(DataFormats.Serializable);

                //Debug.WriteLine($"Dropped task: {insertedTaskItem.TaskName}; Target task: {targetTaskItem.TaskName}");

                if (!_isPreviousLocalOrderStored)
                {
                    _previousLocalOrder.Clear();

                    var vm = (TaskViewModelBase)TaskContainerListBox.DataContext;
                    foreach (var task in vm.LocalTasks!)
                    {
                        _previousLocalOrder.Add(task, vm.LocalTasks!.IndexOf(task));
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
                // drag/drop action was fully completed; update remote order accordingly
                UpdateRemoteOrder();
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

        public void InsertTaskItem(TaskDisplayModel insertedTaskItem, TaskDisplayModel targetTaskItem)
        {
            if (insertedTaskItem == targetTaskItem) { return; }

            var vm = (TaskViewModelBase)TaskContainerListBox.DataContext;
            int oldIndex = vm.LocalTasks!.IndexOf(insertedTaskItem);
            int nextIndex = vm.LocalTasks!.IndexOf(targetTaskItem);

            if (oldIndex != -1 && nextIndex != -1)
            {
                // update local order
                vm.LocalTasks.Move(oldIndex, nextIndex);

                UpdateRemoteOrder();
            }
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
    }
}
