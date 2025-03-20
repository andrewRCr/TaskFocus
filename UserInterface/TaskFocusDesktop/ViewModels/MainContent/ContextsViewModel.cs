using Caliburn.Micro;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaskFocusDesktop.EventModels;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library.Data.Services.Access;
using TaskFocusUI.Library.Data.State;
using TaskFocusUI.Library.Models;
using TaskFocusUI.Library.Utilities;

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

        public async Task HandleAsync(FocusedContextChangedEvent message, CancellationToken cancellationToken)
        {
            FocusedContextId = message.NewFocusedContextId;
            await SetFocusedContextProperties();
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            ShowNoFocusedContextTutorialText = FocusedContextId == null;
        }

        protected override async void LoadLocalTaskData()
        {
            // do not invoke base method; only load relevant context tasks
            await SetFocusedContextProperties();
        }

        private async Task SetFocusedContextProperties()
        {
            if (FocusedContextId != null)
            { await _dataService.FetchRemoteContextAndTasksById((int)FocusedContextId); }

            if (_dataHelper.FocusedContext != null)
            {
                ShowNoFocusedContextTutorialText = false;
                FocusedContextName = _dataHelper.FocusedContext.ContextName.ToUpper();
                var contextTasks = _dataHelper.FocusedContextTasks;
                FocusedContextTasks = new ObservableCollection<TaskDisplayModel>(contextTasks);

                foreach (TaskDisplayModel task in FocusedContextTasks!)
                {
                    task.PropertyChanged += OnExistingTaskPropertyChanged!; // subscribe to property changed event
                }

                TaskCount = FocusedContextTasks.Count;
                UpdateScrollHeight(AppWindowHeight);
            }
            else
            {
                FocusedContextId = null; // may have been deleted
                FocusedContextName = null;
                FocusedContextTasks = null;
            }
        }

        protected override bool HandleDataStateChanged(string propertyName, IDataState dataState)
        {
            if (!dataRefreshTriggers.Contains(propertyName) || ActiveMainContentView != Utilities.ViewCatalog.MainContentView.Contexts)
            {
                return false;
            }

            LoadLocalTaskData();
            Debug.WriteLine("ContextsViewModel: returned true on HandleDataStateChanged!");
            return true;
        }
    }
}