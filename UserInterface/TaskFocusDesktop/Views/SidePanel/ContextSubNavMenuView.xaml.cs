using MaterialDesignThemes.Wpf;
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
using TaskFocusDesktop.ViewModels.SidePanel;
using TaskFocusUI.Library.Models;

namespace TaskFocusDesktop.Views.SidePanel
{
    /// <summary>
    /// Interaction logic for ContextSubNavMenu.xaml
    /// </summary>
    public partial class ContextSubNavMenuView : UserControl
    {
        public ContextSubNavMenuView()
        {
            InitializeComponent();
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
    }
}

