using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TaskFocusDesktop.Commands;
using TaskFocusDesktop.EventModels;
using TaskFocusDesktop.Utilities;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library;
using TaskFocusUI.Library.Models;
using TaskFocusUI.Library.Utilities;
using Windows.ApplicationModel.ExtendedExecution.Foreground;
using Windows.System;

namespace TaskFocusDesktop.ViewModels.SidePanel
{
    public class ProjectSubNavMenuViewModel : TaskViewModelBase
    {
        private Dictionary<ProjectDisplayModel, bool>? _showProjectEditMenuDictionary;
        public Dictionary<ProjectDisplayModel, bool>? ShowProjectEditMenuDictionary
        {
            get { return _showProjectEditMenuDictionary; }
            set
            {
                _showProjectEditMenuDictionary = value;
                NotifyOfPropertyChange(() => ShowProjectEditMenuDictionary);
            }
        }

        public ProjectSubNavMenuViewModel(IEventAggregator events,
                                 IWindowManager window,
                                 IDataState dataState,
                                 IDataService dataService,
                                 IDataHelper dataHelper) : base(events, window, dataState, dataService, dataHelper)
        {
        }

        public RelayCommand SelectedProjectChangedCommand => new RelayCommand(async execute => await OnSelectedProjectChanged());

        public RelayCommand RequestAddNewProjectDialogCommand => new RelayCommand(async execute => await RequestAddNewProjectDialog());

        public RelayCommand OpenSelectedProjectEditMenuCommand => new RelayCommand(async (dataContext) => await OpenSelectedProjectEditMenu((ProjectDisplayModel)dataContext!), canExecute => { return true; });

        public RelayCommand NewHoveredProjectCommand => new RelayCommand((dataContext) => NewHoveredProject((ProjectDisplayModel)dataContext!));

        public RelayCommand NoHoveredProjectsCommand => new RelayCommand(execute => NoHoveredProjects());

        private void NoHoveredProjects()
        {
            if (ShowProjectEditMenuDictionary != null)
            {
                foreach (var project in ShowProjectEditMenuDictionary.Keys)
                {
                    ShowProjectEditMenuDictionary[project] = false;
                }
                NotifyOfPropertyChange(() => ShowProjectEditMenuDictionary);
            }
        }

        private void NewHoveredProject(ProjectDisplayModel hoveredProject)
        {
            if (hoveredProject != null && ShowProjectEditMenuDictionary != null)
            {
                foreach (var project in ShowProjectEditMenuDictionary.Keys)
                {
                    ShowProjectEditMenuDictionary[project] = false;
                }
                ShowProjectEditMenuDictionary[hoveredProject] = true;
                NotifyOfPropertyChange(() => ShowProjectEditMenuDictionary);
                //Debug.WriteLine($"{hoveredProject.ProjectName}'s showBool is {ShowProjectEditMenuDictionary[hoveredProject]}");
            }
        }

        private async Task OnSelectedProjectChanged()
        {
            if (SelectedProject != null) {
                var focusedProjectChangedEvent = new FocusedProjectChangedEvent((int)SelectedProject.Id!, SelectedProject.ProjectName);
                await _events.PublishOnUIThreadAsync(focusedProjectChangedEvent);
            }
        }

        private async Task RequestAddNewProjectDialog()
        {
            var requestShowDialogEvent = new RequestShowDialogEvent(ViewCatalog.DialogView.AddNewProjectDialog);
            await _events.PublishOnUIThreadAsync(requestShowDialogEvent);
        }

        private async Task OpenSelectedProjectEditMenu(ProjectDisplayModel selectedProject)
        {
            //Debug.WriteLine($"projectParam is {selectedProject.ProjectName}");

            // manually set SelectedProject from command param sender's DataContext
            SelectedProject = selectedProject;
            await OnSelectedProjectChanged();

            // open edit menu for SelectedProject
            if (SelectedProject != null)
            {
                Debug.WriteLine($"opening edit menu for {SelectedProject.ProjectName}");
            }
        }

        protected override bool HandleDataStateChanged(string propertyName, IDataState dataState)
        {
            if (!dataRefreshTriggers.Contains(propertyName) || ActiveMainContentView != Utilities.ViewCatalog.MainContentView.Projects)
            {
                return false;
            }

            LoadAllLocalData();
            Debug.WriteLine("ProjectsSubNavMenuViewModel: returned true on HandleDataStateChanged!");
            return true;
        }

        protected override void LoadLocalProjectData()
        {
            base.LoadLocalProjectData();

            // clear previous dictionary and update
            _showProjectEditMenuDictionary = new Dictionary<ProjectDisplayModel, bool>();
            foreach (var project in LocalProjects!)
            {
                _showProjectEditMenuDictionary.Add(project, false);
            }
        }
    }
}
