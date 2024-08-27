using AutoMapper;
using Caliburn.Micro;
using System;
using System.ComponentModel;
using System.Dynamic;
using System.Windows;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusDesktop.Utilities;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Utilities;
using TaskFocusUI.Library;
using System.Collections.Generic;
using TaskFocusUI.Library.Models;
using System.Linq;
using TaskFocusDesktop.EventModels;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.ObjectModel;

namespace TaskFocusDesktop.ViewModels.MainContent
{
    public class ProjectsViewModel : TaskViewModelBase, INotifyPropertyChanged, IHandle<FocusedProjectChangedEvent>
    {
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


        public ProjectsViewModel(IEventAggregator events,
                                 IWindowManager window,
                                 IDataState dataState,
                                 IDataService dataService,
                                 IDataHelper dataHelper) : base(events, window, dataState, dataService, dataHelper)
        {
            OrderingIndex = "ProjectIndex";
            _events.SubscribeOnPublishedThread(this);
        }

        public async Task HandleAsync(FocusedProjectChangedEvent message, CancellationToken cancellationToken)
        {
            FocusedProjectId = message.NewFocusedProjectId;
            await SetFocusedProjectProperties();
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            if (IsLocalDataLoaded())
            {
                ShowNoFocusedProjectTutorialText = FocusedProjectId == null;
            }
        }

        private async Task SetFocusedProjectProperties()
        {
            if (FocusedProjectId != null)
            {
                ShowNoFocusedProjectTutorialText = false;
                await _dataService.FetchRemoteProjectAndTasksById((int)FocusedProjectId);
                FocusedProjectName = _dataHelper.FocusedProject.ProjectName;
                var projectTasks = _dataHelper.FocusedProjectTasks;
                FocusedProjectTasks = new ObservableCollection<TaskDisplayModel>(projectTasks);
            }
            else
            {
                FocusedProjectName = null;
                FocusedProjectTasks = null;
            }
        }
    }
}
