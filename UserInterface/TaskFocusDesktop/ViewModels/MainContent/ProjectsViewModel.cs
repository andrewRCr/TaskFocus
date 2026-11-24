using Caliburn.Micro;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using TaskFocusDesktop.EventModels;
using TaskFocusDesktop.Utilities;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library.Data.Services.Access;
using TaskFocusUI.Library.Data.State;
using TaskFocusUI.Library.Data.Utilities;
using TaskFocusUI.Library.Models;

namespace TaskFocusDesktop.ViewModels.MainContent
{
    public class ProjectsViewModel : TaskViewModelBase, INotifyPropertyChanged, IHandle<FocusedProjectChangedEvent>
    {
        public ProjectsViewModel(IEventAggregator events,
                                IAppState appState,
                                IWindowManager window,
                                IDataState dataState,
                                IDataService dataService,
                                IDataHelper dataHelper) : base(events, appState, window, dataState, dataService, dataHelper)
        {
            OrderingIndex = "ProjectIndex";
            _events.SubscribeOnPublishedThread(this);
        }

        private bool _showNoFocusedProjectTutorialText = false;
        public bool ShowNoFocusedProjectTutorialText
        {
            get { return _showNoFocusedProjectTutorialText; }
            set
            {
                _showNoFocusedProjectTutorialText = value;
                NotifyOfPropertyChange(() => ShowNoFocusedProjectTutorialText);
            }
        }

        private int? _focusedProjectId;
        public int? FocusedProjectId
        {
            get { return _focusedProjectId; }
            set 
            { 
                _focusedProjectId = value; 
                NotifyOfPropertyChange(() => FocusedProjectId);
            }
        }

        private string? _focusedProjectName;
        public string? FocusedProjectName
        {
            get { return _focusedProjectName; }
            set 
            { 
                _focusedProjectName = value;
                NotifyOfPropertyChange(() => FocusedProjectName);
            }
        }

        private ObservableCollection<TaskDisplayModel>?   _focusedProjectTasks;
        public ObservableCollection<TaskDisplayModel>? FocusedProjectTasks
        {
            get { return _focusedProjectTasks; }
            set 
            { 
                _focusedProjectTasks = value; 
                NotifyOfPropertyChange(() => FocusedProjectTasks);
            }
        }

        protected override void LoadLocalTaskData()
        {
            var selectedId = FocusedProjectId;
            var project = selectedId.HasValue
                ? _dataService.GetDataStateProjects()?.Find(x => x.Id == selectedId)
                : null;

            UnsubscribeFromTaskPropertyChangedEvents(FocusedProjectTasks);
            if (project?.Id is int id)
            {
                var tasks = _dataService.GetDataStateTasks();
                FocusedProjectName = project.ProjectName.ToUpper();
                FocusedProjectTasks = new ObservableCollection<TaskDisplayModel>(tasks?.GetProjectTasks(id) ?? []);
                SubscribeToTaskPropertyChangedEvents(FocusedProjectTasks);
            }
            else
            {
                FocusedProjectId = null;
                FocusedProjectName = null;
                FocusedProjectTasks = null;
            }

            ShowNoFocusedProjectTutorialText = project == null;
            LocalTasks = FocusedProjectTasks;
            TaskCount = LocalTasks?.Count ?? 0;
            UpdateScrollHeight(AppWindowHeight);
        }

        public Task HandleAsync(FocusedProjectChangedEvent message, CancellationToken cancellationToken)
        {
            FocusedProjectId = message.NewFocusedProjectId;
            LoadLocalTaskData();
            return Task.CompletedTask;
        }

        protected override bool HandleDataStateChanged(string propertyName, IDataState dataState)
        {
            if (!dataRefreshTriggers.Contains(propertyName) || ActiveMainContentView != Utilities.ViewCatalog.MainContentView.Projects)
            {
                return false;
            }

            LoadLocalTaskData();
            //_logger.Info("ProjectsViewModel: returned true on HandleDataStateChanged!");
            return true;
        }

        // ViewSwitchedEvent handler
        public override async Task HandleAsync(ViewSwitchedEvent message, CancellationToken cancellationToken)
        {
            await base.HandleAsync(message, cancellationToken);

            if (message.SwitchedContentPanel == ViewCatalog.ContentPanel.MainContent &&
                ActiveMainContentView == ViewCatalog.MainContentView.Projects)
            {
                LoadLocalTaskData();
            }
        }
    }
}
