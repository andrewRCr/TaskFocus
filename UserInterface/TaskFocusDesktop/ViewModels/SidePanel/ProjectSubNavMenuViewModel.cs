using Caliburn.Micro;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TaskFocusDesktop.Commands;
using TaskFocusDesktop.EventModels;
using TaskFocusDesktop.Utilities;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library.Data.State;
using TaskFocusUI.Library.Models;
using TaskFocusUI.Library.Data.Services.Access;
using TaskFocusUI.Library.Data.Utilities;

namespace TaskFocusDesktop.ViewModels.SidePanel
{
    public class ProjectSubNavMenuViewModel : TaskViewModelBase, INotifyPropertyChanged
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
        protected override void UpdateScrollHeight(int appWindowHeight)
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
            if (_dataService.IsDataStateLoaded())
            {
                LocalProjects = new ObservableCollection<ProjectDisplayModel>(_dataService.GetDataStateProjects()!.OrderBy(x => x.OrderIndex).ToList());
                SubscribeToProjectPropertyChangedEvents(LocalProjects);

                ProjectCount = LocalProjects.Count;
                UpdateScrollHeight(AppWindowHeight);
            }
        }

        // saves updated project data to server on property change
        protected override async void OnExistingProjectPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            await VerifyAuthAndRedirectIfExpired();

            string? changedProperty = e.PropertyName;
            ProjectDisplayModel senderProject = (ProjectDisplayModel)sender;
            _logger.Info($"{senderProject.ProjectName}'s property {changedProperty} was changed.");

            if (!_dataService.IsProjectCurrentlyBeingUpdated(senderProject))
            {
                await _dataService.UpdateProjectData(senderProject);
            }

            // if a reorder update, need to prevent a remote data fetch until after the entire collection
            // has been updated. CanUpdateOrderIndices will only be true on the final task in collection
            //if (changedProperty!.Contains("Index"))
            //{
            //    if (!CanUpdateOrderingIndices) { return; }
            //    else
            //    {
            //        List<ProjectDisplayModel> projectsToUpdate = LocalProjects!.ToList();
            //        _dataService.UpdateProjectsOrderingIndices(projectsToUpdate);
            //    }
            //}
            //else { await _dataService.UpdateProjectData(senderProject); }
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
    }
}
