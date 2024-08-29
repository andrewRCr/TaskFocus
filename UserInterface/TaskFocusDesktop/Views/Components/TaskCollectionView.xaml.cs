using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using TaskFocusUI.Library.Models;

namespace TaskFocusDesktop.Views.Components
{
    /// <summary>
    /// Interaction logic for TaskCollectionView.xaml
    /// </summary>
    public partial class TaskCollectionView : UserControl
    {
        public static readonly DependencyProperty CollectionNameProperty =
            DependencyProperty.Register("CollectionName", typeof(string), typeof(TaskCollectionView), new PropertyMetadata(string.Empty));

        public string CollectionName
        {
            get { return (string)GetValue(CollectionNameProperty); }
            set { SetValue(CollectionNameProperty, value); }
        }

        public static readonly DependencyProperty CollectionIconKindProperty =
            DependencyProperty.Register("CollectionIconKind", typeof(PackIconKind), typeof(TaskCollectionView), new PropertyMetadata(null));

        public PackIconKind CollectionIconKind  
        {
            get { return (PackIconKind)GetValue(CollectionIconKindProperty); }
            set { SetValue(CollectionIconKindProperty, value); }
        }

        public static readonly DependencyProperty CollectionIdProperty =
            DependencyProperty.Register("CollectionId", typeof(int), typeof(TaskCollectionView), new PropertyMetadata(null));

        public int? CollectionId
        {
            get { return (int?)GetValue(CollectionIdProperty); }
            set { SetValue(CollectionIdProperty, value); }
        }

        public static readonly DependencyProperty IsProjectCollectionProperty =
            DependencyProperty.Register("IsProjectCollection", typeof(bool), typeof(TaskCollectionView), new PropertyMetadata(false));

        public bool IsProjectCollection
        {
            get { return (bool)GetValue(IsProjectCollectionProperty); }
            set { SetValue(IsProjectCollectionProperty, value); }
        }

        public static readonly DependencyProperty CollectionTasksProperty =
            DependencyProperty.Register("CollectionTasks", typeof(ObservableCollection<TaskDisplayModel>), typeof(TaskCollectionView), new PropertyMetadata(null));

        public ObservableCollection<TaskDisplayModel> CollectionTasks
        {
            get { return (ObservableCollection<TaskDisplayModel>)GetValue(CollectionTasksProperty); }
            set { SetValue(CollectionTasksProperty, value); }
        }

        //public static readonly DependencyProperty FilteredCollectionTasksProperty =
        //    DependencyProperty.Register("FilteredCollectionTasks", typeof(ObservableCollection<TaskDisplayModel>), typeof(TaskCollectionView), new PropertyMetadata(null));

        //public ObservableCollection<TaskDisplayModel> FilteredCollectionTasks
        //{
        //    get { return (ObservableCollection<TaskDisplayModel>)GetValue(FilteredCollectionTasksProperty); }
        //    set { SetValue(FilteredCollectionTasksProperty, value); }
        //}

        public TaskCollectionView()
        {
            InitializeComponent();
        }

        //public void FilterCollectionTasks()
        //{
        //    FilteredCollectionTasks = new ObservableCollection<TaskDisplayModel>();

        //    foreach (var task in CollectionTasks)
        //    {
        //        if (IsProjectCollection)
        //        {
        //            if (task.ProjectId == CollectionId)
        //            {
        //                FilteredCollectionTasks.Add(task);
        //            }
        //        }
        //        else
        //        {
        //            if (task.ContextId == CollectionId)
        //            {
        //                FilteredCollectionTasks.Add(task);
        //            }
        //        }
        //    }
        //}
    }
}
