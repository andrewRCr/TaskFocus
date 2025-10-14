using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TaskFocusDesktop.ViewModels.SidePanel;
using TaskFocusUI.Library.Models;

namespace TaskFocusDesktop.Views.SidePanel
{
    /// <summary>
    /// Interaction logic for ContextSubNavMenu.xaml
    /// </summary>
    public partial class ContextSubNavMenuView : UserControl
    {
        private bool _isPreviousLocalOrderStored = false;
        private Dictionary<ContextDisplayModel, int> _previousLocalOrder;
        private string _orderingIndex = "OrderIndex";

        private object? _lastSelection;

        public static readonly DependencyProperty IsDraggingProperty =
            DependencyProperty.Register("IsDragging", typeof(bool), typeof(ContextSubNavMenuView), new PropertyMetadata(false));

        public bool IsDragging
        {
            get { return (bool)GetValue(IsDraggingProperty); }
            set { SetValue(IsDraggingProperty, value); }
        }

        public ContextSubNavMenuView()
        {
            InitializeComponent();
            _previousLocalOrder = new Dictionary<ContextDisplayModel, int>();
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
                var context = (ContextDisplayModel)item.DataContext;
                var vm = (ContextSubNavMenuViewModel)subNavMenuListBox.DataContext;
                vm.SelectedContext = context;
                item.IsSelected = true;
            }
        }

        private void ListItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = sender as ListBoxItem;
            if (item != null)
            {
                var context = (ContextDisplayModel)item.DataContext;
                var vm = (ContextSubNavMenuViewModel)subNavMenuListBox.DataContext;

                if (item.IsSelected && vm.SelectedContext == context)
                {
                    ContextMenu? cm = FindResource("collectionEditContextMenu") as ContextMenu;
                    if (cm != null)
                    {
                        cm.PlacementTarget = sender as ListBoxItem;
                        cm.IsOpen = true;
                    }
                }
                else if (vm.SelectedContext == context)
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
                    vm.SelectedContext = context;
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

                var context = (ContextDisplayModel)item.DataContext;
                var vm = (ContextSubNavMenuViewModel)subNavMenuListBox.DataContext;
                vm.SelectedContext = context;
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
                var context = (ContextDisplayModel)item.DataContext;
                var vm = (ContextSubNavMenuViewModel)subNavMenuListBox.DataContext;

                if (item.IsSelected && vm.SelectedContext == context)
                {
                    ContextMenu? cm = FindResource("collectionEditContextMenu") as ContextMenu;
                    if (cm != null)
                    {
                        cm.PlacementTarget = sender as ListBoxItem;
                        cm.IsOpen = true;
                    }
                }
                else if (vm.SelectedContext == context)
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
                    vm.SelectedContext = context;
                    item.IsSelected = true;
                }
            }
        }

        private void StackPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            this.addNewContextText.Visibility = Visibility.Visible;
        }

        private void StackPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            this.addNewContextText.Visibility = Visibility.Hidden;
        }

        private void ListItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && sender is FrameworkElement frameworkElement)
            {
                // flag for highlighting
                IsDragging = true;
                //Debug.WriteLine("MOUSE DOWN - DRAGGING");

                object contextItem = frameworkElement.DataContext;
                DragDropEffects dragDropResult = DragDrop.DoDragDrop(frameworkElement,
                    new DataObject(DataFormats.Serializable, contextItem), DragDropEffects.Move);

                if (dragDropResult == DragDropEffects.None)
                {
                    UndoPreviewInsertContextItem();
                    IsDragging = false;
                    //Debug.WriteLine("MOUSE UP - DRAGGING STOPPED");
                }
            }
        }

        private void StoreLocalOrder()
        {
            _previousLocalOrder.Clear();

            var vm = (ContextSubNavMenuViewModel)subNavMenuListBox.DataContext;
            foreach (var context in vm.LocalContexts!) { _previousLocalOrder.Add(context, vm.LocalContexts!.IndexOf(context)); }

            _isPreviousLocalOrderStored = true;
        }


        private void ListItem_PreviewDragEnter(object sender, DragEventArgs e)
        {
            if (sender is FrameworkElement frameworkElement)
            {
                var targetContextItem = (ContextDisplayModel)frameworkElement.DataContext; // task dropped onto
                var insertedContextItem = (ContextDisplayModel)e.Data.GetData(DataFormats.Serializable); // dropped task

                if (!_isPreviousLocalOrderStored) { StoreLocalOrder(); }
                PreviewInsertContextItem(insertedContextItem, targetContextItem);
            }
        }

        private void ListItem_PreviewDragLeave(object sender, DragEventArgs e)
        {
            HitTestResult result = VisualTreeHelper.HitTest(subNavMenuListBox, e.GetPosition(subNavMenuListBox));

            if (result == null)
            {
                // if dragged out of container entirely, revert local order
                UndoPreviewInsertContextItem();
            }
        }

        private void ListItem_PreviewDrop(object sender, DragEventArgs e)
        {
            if (sender is FrameworkElement frameworkElement)
            {
                // remove visual highlighting flag
                IsDragging = false;
                //Debug.WriteLine("DRAGGING STOPPED");

                // drag/drop action was fully completed; update remote order accordingly
                var vm = (ContextSubNavMenuViewModel)subNavMenuListBox.DataContext;
                var insertedContextItem = (ContextDisplayModel)e.Data.GetData(DataFormats.Serializable);
                int previousIndex = _previousLocalOrder[insertedContextItem];

                bool orderChanged = previousIndex != vm.LocalContexts!.IndexOf(insertedContextItem);
                if (orderChanged) { UpdateRemoteOrder(); }
            }
        }

        public void PreviewInsertContextItem(ContextDisplayModel insertedContextItem, ContextDisplayModel targetContextItem)
        {
            if (insertedContextItem == targetContextItem) { return; }

            var vm = (ContextSubNavMenuViewModel)subNavMenuListBox.DataContext;
            int oldIndex = vm.LocalContexts!.IndexOf(insertedContextItem);
            int nextIndex = vm.LocalContexts!.IndexOf(targetContextItem);

            if (oldIndex != -1 && nextIndex != -1)
            {
                // update local order
                vm.LocalContexts.Move(oldIndex, nextIndex);
            }
        }

        public void UndoPreviewInsertContextItem()
        {
            var vm = (ContextSubNavMenuViewModel)subNavMenuListBox.DataContext;
            foreach (var contextIndexPair in _previousLocalOrder)
            {
                vm.LocalContexts!.Move(vm.LocalContexts.IndexOf(contextIndexPair.Key), _previousLocalOrder[contextIndexPair.Key]);
            }

            _previousLocalOrder.Clear();
            _isPreviousLocalOrderStored = false;
        }

        private void UpdateRemoteOrder()
        {
            var vm = (ContextSubNavMenuViewModel)subNavMenuListBox.DataContext;
            vm.CanUpdateOrderingIndices = false;

            foreach (var item in vm.LocalContexts!)
            {
                if (vm.LocalContexts.IndexOf(item) == vm.LocalContexts.Count - 1)
                {
                    // on last one; can update all collection indices now
                    vm.CanUpdateOrderingIndices = true;
                }

                // will trigger a DataService.UpdateContextsOrderingIndices call
                item[_orderingIndex] = vm.LocalContexts.IndexOf(item);
                //Debug.WriteLine($"{item.ContextName} OrderIndex: {item[_orderingIndex]}");


                _previousLocalOrder.Clear();
                _isPreviousLocalOrderStored = false;
            }
        }

        // when the observable collection LocalContexts is reordered via its Move method (in PreviewInsertContextItem),
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
