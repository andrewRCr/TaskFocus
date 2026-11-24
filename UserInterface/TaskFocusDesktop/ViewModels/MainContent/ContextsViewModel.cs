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
    public class ContextsViewModel : TaskViewModelBase, INotifyPropertyChanged, IHandle<FocusedContextChangedEvent>
    {
        private bool _showNoFocusedContextTutorialText = false;
        public bool ShowNoFocusedContextTutorialText
        {
            get { return _showNoFocusedContextTutorialText; }
            set
            {
                _showNoFocusedContextTutorialText = value;
                NotifyOfPropertyChange(() => ShowNoFocusedContextTutorialText);
            }
        }

        private int? _focusedContextId;
        public int? FocusedContextId
        {
            get { return _focusedContextId; }
            set
            {
                _focusedContextId = value;
                NotifyOfPropertyChange(() => FocusedContextId);
            }
        }

        private string? _focusedContextName;
        public string? FocusedContextName
        {
            get { return _focusedContextName; }
            set
            {
                _focusedContextName = value;
                NotifyOfPropertyChange(() => FocusedContextName);
            }
        }

        private ObservableCollection<TaskDisplayModel>? _focusedContextasks;
        public ObservableCollection<TaskDisplayModel>? FocusedContextTasks
        {
            get { return _focusedContextasks; }
            set
            {
                _focusedContextasks = value;
                NotifyOfPropertyChange(() => FocusedContextTasks);
            }
        }

        public ContextsViewModel(IEventAggregator events,
                                 IAppState appState,
                                 IWindowManager window,
                                 IDataState dataState,
                                 IDataService dataService,
                                 IDataHelper dataHelper) : base(events, appState, window, dataState, dataService, dataHelper)
        {
            OrderingIndex = "ContextIndex";
            _events.SubscribeOnPublishedThread(this);
        }

        protected override void LoadLocalTaskData()
        {
            var selectedId = FocusedContextId;
            var context = selectedId.HasValue
                ? _dataService.GetDataStateContexts()?.Find(x => x.Id == selectedId)
                : null;

            UnsubscribeFromTaskPropertyChangedEvents(FocusedContextTasks);
            if (context?.Id is int id)
            {
                var tasks = _dataService.GetDataStateTasks();
                FocusedContextName = context.ContextName.ToUpper();
                FocusedContextTasks = new ObservableCollection<TaskDisplayModel>(tasks?.GetContextTasks(id) ?? []);
                SubscribeToTaskPropertyChangedEvents(FocusedContextTasks);
            }
            else
            {
                FocusedContextId = null;
                FocusedContextName = null;
                FocusedContextTasks = null;
            }

            ShowNoFocusedContextTutorialText = context == null;
            LocalTasks = FocusedContextTasks;
            TaskCount = LocalTasks?.Count ?? 0;
            UpdateScrollHeight(AppWindowHeight);
        }

        public Task HandleAsync(FocusedContextChangedEvent message, CancellationToken cancellationToken)
        {
            FocusedContextId = message.NewFocusedContextId;
            LoadLocalTaskData();
            return Task.CompletedTask;
        }

        protected override bool HandleDataStateChanged(string propertyName, IDataState dataState)
        {
            if (!dataRefreshTriggers.Contains(propertyName) || ActiveMainContentView != Utilities.ViewCatalog.MainContentView.Contexts)
            {
                return false;
            }

            LoadLocalTaskData();
            //_logger.Info("ContextsViewModel: returned true on HandleDataStateChanged!");
            return true;
        }

        // ViewSwitchedEvent handler
        public override async Task HandleAsync(ViewSwitchedEvent message, CancellationToken cancellationToken)
        {
            await base.HandleAsync(message, cancellationToken);

            if (message.SwitchedContentPanel == ViewCatalog.ContentPanel.MainContent &&
                ActiveMainContentView == ViewCatalog.MainContentView.Contexts)
            {
                LoadLocalTaskData();
            }
        }
    }
}