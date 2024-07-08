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

namespace TaskFocusDesktop.ViewModels.MainContent
{
    public class ProjectsViewModel : TaskViewModelBase, INotifyPropertyChanged
    {
        public ProjectsViewModel(IEventAggregator events,
                                 IWindowManager window,
                                 IDataState dataState,
                                 IDataService dataService,
                                 IDataHelper dataHelper) : base(events, window, dataState, dataService, dataHelper)
        {
            OrderingIndex = "ProjectIndex";
        }

        private BindingList<string>? _projectNames;
        public BindingList<string>? ProjectNames
        {
            get { return _projectNames; }
            set 
            { 
                _projectNames = value; 
                NotifyOfPropertyChange(() => ProjectNames);
            }
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            if (IsLocalDataLoaded())
            {
                ProjectNames = new BindingList<string>();

                foreach (var item in LocalProjects!)
                {
                    ProjectNames!.Add(item.ProjectName);
                }

                NotifyOfPropertyChange(() => ProjectNames);
            }
        }

    }
}
