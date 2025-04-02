using Caliburn.Micro;
using MaterialDesignThemes.Wpf;
using System;
using System.Threading.Tasks;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library.Data.Services.Access;
using TaskFocusUI.Library.Data.State;
using TaskFocusUI.Library.Data.Utilities;
using TaskFocusUI.Library.Models;

namespace TaskFocusDesktop.ViewModels.Dialogs
{
    public class NewProjectDialogViewModel : DialogViewModelBase
    {
        public NewProjectDialogViewModel(IEventAggregator events,
                                         IAppState appState,
                                         IWindowManager window,
                                         IDataState dataState,
                                         IDataService dataService,
                                         IDataHelper dataHelper) : base(events, appState, window, dataState, dataService, dataHelper)
        {
            NewCollectionPlaceholderText = "Project Name";
            HeaderText = "ADD NEW PROJECT";
        }

        protected override void CloseDialog()
        {
            NewCollectionName = null;
            base.CloseDialog();
        }

        protected override async Task ProcessSubmitAction()
        {
            if (string.IsNullOrWhiteSpace(NewCollectionName))
            {
                IsFeedbackError = true;
                FeedbackMessage = "Project name cannot be empty; please try again.";
            }

            else if (!_dataHelper.IsNewProjectNameUnique(NewCollectionName!))
            {
                IsFeedbackError = true;
                FeedbackMessage = "Project names must be unique; please try again.";
            }
            else
            {
                IsFeedbackError = false;
                FeedbackMessage = null;

                var newProject = new ProjectModel { ProjectName = NewCollectionName! };
                await _dataService.AddProject(newProject);

                FeedbackMessage = "Project added!";
                await Task.Delay(TimeSpan.FromSeconds(_successMsgDisplaySec));

                // close dialog
                DialogHost.Close(_dialogIdentifier);
                FeedbackMessage = null;
                NewCollectionName = null;
            }
        }
    }
}
