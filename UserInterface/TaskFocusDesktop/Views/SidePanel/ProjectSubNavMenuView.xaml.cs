using Caliburn.Micro;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using TaskFocusDesktop.Commands;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusDesktop.ViewModels.MainContent;
using TaskFocusDesktop.ViewModels.SidePanel;
using TaskFocusDesktop.Views.Components;
using TaskFocusUI.Library.Models;

namespace TaskFocusDesktop.Views.SidePanel
{
    /// <summary>
    /// Interaction logic for ProjectSubNavMenuView.xaml
    /// </summary>
    public partial class ProjectSubNavMenuView : UserControl
    {
        public ProjectSubNavMenuView()
        {
            InitializeComponent();
            _previousLocalOrder = new Dictionary<ProjectDisplayModel, int>();
        }

        private void ListItemTextBlock_SelectParentListBoxItem(object sender, RoutedEventArgs e)
        {
            var textBlock = sender as TextBlock;
            var parent = VisualTreeHelper.GetParent(textBlock);
            while (!(parent is ListBoxItem))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            var item = parent as ListBoxItem;
            if (item != null)
            {
                var project = (ProjectDisplayModel)item.DataContext;
                var vm = (ProjectSubNavMenuViewModel)subNavMenuListBox.DataContext;
                vm.SelectedProject = project;
                item.IsSelected = true;
            }
        }

        private void ListItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = sender as ListBoxItem;
            if (item != null)
            {
                var project = (ProjectDisplayModel)item.DataContext;
                var vm = (ProjectSubNavMenuViewModel)subNavMenuListBox.DataContext;

                if (item.IsSelected && vm.SelectedProject == project)
                {
                    ContextMenu? cm = FindResource("collectionEditContextMenu") as ContextMenu;
                    if (cm != null)
                    {
                        cm.PlacementTarget = sender as ListBoxItem;
                        cm.IsOpen = true;
                    }
                }
                else if (vm.SelectedProject == project)
                {
                    item.IsSelected = true;

                    ContextMenu? cm = FindResource("collectionEditContextMenu") as ContextMenu;
                    if (cm != null)
                    {
                        cm.PlacementTarget = sender as ListBoxItem;
                        cm.IsOpen = true;
                    }
                }
                else
                {
                    vm.SelectedProject = project;
                    item.IsSelected = true;
                }
            }
        }

        private void ListItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = sender as ListBoxItem;
            if (item != null)
            {
                List<Type> typesToIgnore = new() {
                     typeof(PackIcon), typeof(Grid) };

                // ignore if sent from editMenuButtonGrid (will be handled by its own event handler)
                if (typesToIgnore.Contains(e.Source.GetType()))
                {
                    return;
                }

                var project = (ProjectDisplayModel)item.DataContext;
                var vm = (ProjectSubNavMenuViewModel)subNavMenuListBox.DataContext;
                vm.SelectedProject = project;
                item.IsSelected = true;
            }
        }

        private void editMenuButtonGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var buttonGrid = sender as Grid;
            if (buttonGrid == null) { return; }

            buttonGrid.Visibility = Visibility.Visible;

            var parent = VisualTreeHelper.GetParent(buttonGrid);
            while (!(parent is ListBoxItem))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            var item = parent as ListBoxItem;
            if (item != null)
            {
                var project = (ProjectDisplayModel)item.DataContext;
                var vm = (ProjectSubNavMenuViewModel)subNavMenuListBox.DataContext;

                if (item.IsSelected && vm.SelectedProject == project)
                {
                    ContextMenu? cm = FindResource("collectionEditContextMenu") as ContextMenu;
                    if (cm != null)
                    {
                        cm.PlacementTarget = sender as ListBoxItem;
                        cm.IsOpen = true;
                    }
                }
                else if (vm.SelectedProject == project)
                {
                    item.IsSelected = true;

                    ContextMenu? cm = FindResource("collectionEditContextMenu") as ContextMenu;
                    if (cm != null)
                    {
                        cm.PlacementTarget = sender as ListBoxItem;
                        cm.IsOpen = true;
                    }
                }
                else
                {
                    vm.SelectedProject = project;
                    item.IsSelected = true;
                }
            }
        }

        private void StackPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            this.addNewProjectText.Visibility = Visibility.Visible;
        }

        private void StackPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            this.addNewProjectText.Visibility = Visibility.Hidden;
        }





        /// DRAG / DROP

        private bool _isPreviousLocalOrderStored = false;
        private Dictionary<ProjectDisplayModel, int> _previousLocalOrder;
        private string _orderingIndex = "OrderIndex";

        private object? _lastSelection;

        public static readonly DependencyProperty IsDraggingProperty =
            DependencyProperty.Register("IsDragging", typeof(bool), typeof(ProjectSubNavMenuView), new PropertyMetadata(false));

        public bool IsDragging
        {
            get { return (bool)GetValue(IsDraggingProperty); }
            set { SetValue(IsDraggingProperty, value); }
        }

        public static readonly DependencyProperty ProjectItemInsertedCommandProperty =
            DependencyProperty.Register("ProjectItemInsertedCommand", typeof(RelayCommand), typeof(ProjectSubNavMenuView), new PropertyMetadata(null));

        public RelayCommand ProjectItemInsertedCommand
        {
            get { return (RelayCommand)GetValue(ProjectItemInsertedCommandProperty); }
            set { SetValue(ProjectItemInsertedCommandProperty, value); }
        }

        private void ListItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && sender is FrameworkElement frameworkElement)
            {
                //List<Type> typesToIgnore = new() {
                //    typeof(TextBox), typeof(Button), typeof(Border), typeof(Path) };
                //string textBoxViewStr = "System.Windows.Controls.TextBoxView"; // internal WPF component; no public API

                //// ignore drag if clicked on any controls
                //if (typesToIgnore.Contains(e.OriginalSource.GetType()) || e.OriginalSource.ToString() == textBoxViewStr)
                //{
                //    e.Handled = true;
                //    //Debug.WriteLine($"OriginalSourceType = {e.OriginalSource.GetType()}; IGNORED");
                //    return;
                //}

                // flag for highlighting
                IsDragging = true;
                Debug.WriteLine("MOUSE DOWN - DRAGGING");

                object projectItem = frameworkElement.DataContext;
                DragDropEffects dragDropResult = DragDrop.DoDragDrop(frameworkElement,
                    new DataObject(DataFormats.Serializable, projectItem), DragDropEffects.Move);

                if (dragDropResult == DragDropEffects.None)
                {
                    UndoPreviewInsertProjectItem();
                    IsDragging = false;
                    Debug.WriteLine("MOUSE UP - DRAGGING STOPPED");
                }
            }
        }

        private void StoreLocalOrder()
        {
            _previousLocalOrder.Clear();

            var vm = (ProjectSubNavMenuViewModel)subNavMenuListBox.DataContext;
            foreach (var project in vm.LocalProjects!) { _previousLocalOrder.Add(project, vm.LocalProjects!.IndexOf(project)); }

            _isPreviousLocalOrderStored = true;
        }


        private void ListItem_PreviewDragEnter(object sender, DragEventArgs e)
        {
            if (sender is FrameworkElement frameworkElement)
            {
                var targetProjectItem = (ProjectDisplayModel)frameworkElement.DataContext; // task dropped onto
                var insertedProjectItem = (ProjectDisplayModel)e.Data.GetData(DataFormats.Serializable); // dropped task

                if (!_isPreviousLocalOrderStored) { StoreLocalOrder(); }
                PreviewInsertProjectItem(insertedProjectItem, targetProjectItem);
            }
        }

        private void ListItem_PreviewDragLeave(object sender, DragEventArgs e)
        {
            HitTestResult result = VisualTreeHelper.HitTest(subNavMenuListBox, e.GetPosition(subNavMenuListBox));

            if (result == null)
            {
                // if dragged out of container entirely, revert local order
                UndoPreviewInsertProjectItem();
            }
        }

        private void ListItem_PreviewDrop(object sender, DragEventArgs e)
        {
            if (sender is FrameworkElement frameworkElement)
            {
                // remove visual highlighting flag
                IsDragging = false;
                Debug.WriteLine("DRAGGING STOPPED");

                // drag/drop action was fully completed; update remote order accordingly
                var vm = (ProjectSubNavMenuViewModel)subNavMenuListBox.DataContext;
                var insertedProjectItem = (ProjectDisplayModel)e.Data.GetData(DataFormats.Serializable);
                int previousIndex = _previousLocalOrder[insertedProjectItem];

                bool orderChanged = previousIndex != vm.LocalProjects!.IndexOf(insertedProjectItem);
                if (orderChanged) { UpdateRemoteOrder(); }
            }
        }

        public void PreviewInsertProjectItem(ProjectDisplayModel insertedProjectItem, ProjectDisplayModel targetProjectItem)
        {
            if (insertedProjectItem == targetProjectItem) { return; }

            var vm = (ProjectSubNavMenuViewModel)subNavMenuListBox.DataContext;
            int oldIndex = vm.LocalProjects!.IndexOf(insertedProjectItem);
            int nextIndex = vm.LocalProjects!.IndexOf(targetProjectItem);

            if (oldIndex != -1 && nextIndex != -1)
            {
                // update local order
                vm.LocalProjects.Move(oldIndex, nextIndex);
            }
        }

        public void UndoPreviewInsertProjectItem()
        {
            var vm = (ProjectSubNavMenuViewModel)subNavMenuListBox.DataContext;
            foreach (var projectIndexPair in _previousLocalOrder)
            {
                vm.LocalProjects!.Move(vm.LocalProjects.IndexOf(projectIndexPair.Key), _previousLocalOrder[projectIndexPair.Key]);
            }

            _previousLocalOrder.Clear();
            _isPreviousLocalOrderStored = false;
        }

        private void UpdateRemoteOrder()
        {
            var vm = (ProjectSubNavMenuViewModel)subNavMenuListBox.DataContext;
            vm.CanUpdateOrderingIndices = false;

            foreach (var item in vm.LocalProjects!)
            {
                if (vm.LocalProjects.IndexOf(item) == vm.LocalProjects.Count - 1)
                {
                    // on last one; can update all collection indices now
                    vm.CanUpdateOrderingIndices = true;
                }

                // will trigger a DataService.UpdateProjectsOrderingIndices call
                item[_orderingIndex] = vm.LocalProjects.IndexOf(item);
                Debug.WriteLine($"{item.ProjectName} OrderIndex: {item[_orderingIndex]}");


                _previousLocalOrder.Clear();
                _isPreviousLocalOrderStored = false;
            }
        }

        // when the observable collection LocalProjects is reordered via its Move method (in PreviewInsertProjectItem),
        // moved items are temporarily removed before being re-inserted. when these are also the selected item, WPF
        // will "prematurely" set the SelectedItem to null and selection will not be preserved once the item is
        // re-added at its new index. this workaround preserves that selection.
        private void subNavMenuListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems != null && (e.AddedItems == null || e.AddedItems.Count == 0))
            {
                _lastSelection = e.RemovedItems.OfType<object>().FirstOrDefault()!;
                subNavMenuListBox.SelectedItem = _lastSelection;
            }
        }
    }
}
