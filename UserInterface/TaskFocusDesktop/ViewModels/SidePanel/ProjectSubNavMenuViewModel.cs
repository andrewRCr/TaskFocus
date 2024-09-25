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
        public ProjectSubNavMenuViewModel(IEventAggregator events,
                                 IWindowManager window,
                                 IDataState dataState,
                                 IDataService dataService,
                                 IDataHelper dataHelper) : base(events, window, dataState, dataService, dataHelper)
        {
        }

        public RelayCommand SelectedProjectChangedCommand => new RelayCommand(async execute => await OnSelectedProjectChanged());

        public RelayCommand RequestAddNewProjectDialogCommand => new RelayCommand(async execute => await RequestAddNewProjectDialog());

        public RelayCommand RequestDeleteSelectedProjectDialogCommand => new RelayCommand(async execute => await RequestDeleteSelectedProjectDialog());

        public RelayCommand RequestRenameSelectedProjectDialogCommand => new RelayCommand(async execute => await RequestRenameSelectedProjectDialog());

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
