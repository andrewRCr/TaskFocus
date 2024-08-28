using AutoMapper;
using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TaskFocusDesktop.EventModels;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library;
using TaskFocusUI.Library.API;
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
                                 IWindowManager window,
                                 IDataState dataState,
                                 IDataService dataService,
                                 IDataHelper dataHelper) : base(events, window, dataState, dataService, dataHelper)
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

            if (IsLocalDataLoaded())
            {
                ShowNoFocusedContextTutorialText = FocusedContextId == null;
            }
        }

        private async Task SetFocusedContextProperties()
        {
            if (FocusedContextId != null)
            {
                ShowNoFocusedContextTutorialText = false;
                await _dataService.FetchRemoteContextAndTasksById((int)FocusedContextId);
                FocusedContextName = _dataHelper.FocusedContext.ContextName;
                var contextTasks = _dataHelper.FocusedContextTasks;
                FocusedContextTasks = new ObservableCollection<TaskDisplayModel>(contextTasks);
            }
            else
            {
                FocusedContextName = null;
                FocusedContextTasks = null;
            }
        }
    }
}