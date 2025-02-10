using Caliburn.Micro;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TaskFocusDesktop.Commands;
using TaskFocusDesktop.EventModels;
using TaskFocusDesktop.Utilities;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library;
using TaskFocusUI.Library.Models;
using TaskFocusUI.Library.Utilities;

namespace TaskFocusDesktop.ViewModels.SidePanel
{
    public class ProjectSubNavMenuViewModel : TaskViewModelBase, IHandle<AppWindowHeightChangedEvent>
    {
        private int _projectCount;
        public int ProjectCount
        {
            get { return _projectCount; }
            set
            {
                _projectCount = value;
                NotifyOfPropertyChange(() => ProjectCount);
            }
        }

        private int _maxSubNavMenuHeight;
        public int MaxSubNavMenuHeight
        {
            get { return _maxSubNavMenuHeight; }
            set
            {
                _maxSubNavMenuHeight = value;
                NotifyOfPropertyChange(() => MaxSubNavMenuHeight);
            }
        }

        private int _listBoxHeight;
        public int ListBoxHeight
        {
            get { return _listBoxHeight; }
            set
            {
                _listBoxHeight = value;
                NotifyOfPropertyChange(() => ListBoxHeight);
            }
        }

        private int _appWindowHeight;
        public int AppWindowHeight
        {
            get { return _appWindowHeight; }
            set
            {
                _appWindowHeight = value;
                NotifyOfPropertyChange(() => AppWindowHeight);
            }
        }

        public ProjectSubNavMenuViewModel(IEventAggregator events,
                                          IAppState appState,
                                          IWindowManager window,
                                          IDataState dataState,
                                          IDataService dataService,
                                          IDataHelper dataHelper) : base(events, appState, window, dataState, dataService, dataHelper)
        {
            AppWindowHeight = (int)appState.AppWindowHeight;
        }

        public RelayCommand SelectedProjectChangedCommand => new RelayCommand(async execute => await OnSelectedProjectChanged());

        public RelayCommand RequestAddNewProjectDialogCommand => new RelayCommand(async execute => await RequestAddNewProjectDialog());

        public RelayCommand RequestDeleteSelectedProjectDialogCommand => new RelayCommand(async execute => await RequestDeleteSelectedProjectDialog());

        public RelayCommand RequestRenameSelectedProjectDialogCommand => new RelayCommand(async execute => await RequestRenameSelectedProjectDialog());

        // updates project listbox and containing scrollviewer height values dynamically
        private void UpdateScrollHeight(int appWindowHeight)
        {
            int fixedBaseSubMenuHeight = 110;
            int fixedTotalOtherWindowElementsHeight = 300;
            int requiredProjectListHeight = 36 * ProjectCount;
            MaxSubNavMenuHeight = requiredProjectListHeight + fixedBaseSubMenuHeight;

            if (appWindowHeight - fixedTotalOtherWindowElementsHeight < requiredProjectListHeight)
            {
                int difference = requiredProjectListHeight - (appWindowHeight - fixedTotalOtherWindowElementsHeight);
                ListBoxHeight = requiredProjectListHeight - difference;
            }
            else
            {
                ListBoxHeight = requiredProjectListHeight;
            }

            AppWindowHeight = appWindowHeight;
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

        private async Task RequestRenameSelectedProjectDialog()
        {
            var requestShowDialogEvent = new RequestShowDialogEvent(ViewCatalog.DialogView.RenameProjectDialog);
            await _events.PublishOnUIThreadAsync(requestShowDialogEvent);
        }

        private async Task RequestDeleteSelectedProjectDialog()
        {
            var requestShowDialogEvent = new RequestShowDialogEvent(ViewCatalog.DialogView.DeleteProjectDialog);
            await _events.PublishOnUIThreadAsync(requestShowDialogEvent);
        }

        protected override void LoadLocalProjectData()
        {
            if (_dataState.IsDataLoaded())
            {
                LocalProjects = new ObservableCollection<ProjectDisplayModel>(_dataState.Projects!);
                foreach (ProjectDisplayModel project in LocalProjects!)
                {
                    project.PropertyChanged += OnExistingProjectPropertyChanged!; // subscribe to property changed event
                }
                ProjectCount = LocalProjects.Count;
                UpdateScrollHeight(AppWindowHeight);
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

        public Task HandleAsync(AppWindowHeightChangedEvent message, CancellationToken cancellationToken)
        {
            UpdateScrollHeight((int)message.NewAppWindowHeight);
            return Task.CompletedTask;
        }
    }
}
