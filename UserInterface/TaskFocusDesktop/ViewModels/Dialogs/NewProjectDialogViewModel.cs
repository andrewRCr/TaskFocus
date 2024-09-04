using Caliburn.Micro;
using MaterialDesignThemes.Wpf;
using Nextended.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskFocusDesktop.Commands;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library.Models;
using TaskFocusUI.Library.Utilities;

namespace TaskFocusDesktop.ViewModels.Dialogs
{
    public class NewProjectDialogViewModel : DialogViewModelBase
    {
        private string? _newProjectName;
        public string? NewProjectName
        {
            get { return _newProjectName; }
            set 
            {
                _newProjectName = value; 
                NotifyOfPropertyChange(() => NewProjectName);
            }
        }

        public RelayCommand ProcessAddNewProjectCommand => new RelayCommand(async execute => await ProcessAddNewProject());

        public NewProjectDialogViewModel(IEventAggregator events, IDataService dataService, IDataHelper dataHelper) : base(events, dataService, dataHelper)
        {        
        }

        private async Task ProcessAddNewProject()
        {
            if (NewProjectName.IsNullOrWhiteSpace())
            {
                IsFeedbackError = true;
                FeedbackMessage = "Project name cannot be empty; please try again.";     
            }

            else if (!_dataHelper.IsNewProjectNameUnique(NewProjectName!))
            {
                IsFeedbackError = true;
                FeedbackMessage = "Project names must be unique; please try again.";
            }
            else
            {     
                IsFeedbackError = false;
                FeedbackMessage = null;

                var newProject = new ProjectModel { ProjectName = NewProjectName! };
                await _dataService.AddProject(newProject);

                FeedbackMessage = "Project added!";
                await Task.Delay(TimeSpan.FromSeconds(1));

                // close dialog
                DialogHost.Close(_dialogIdentifier);
                FeedbackMessage = null;
                NewProjectName = null;
            }
        }

        protected override void CloseDialog()
        {
            NewProjectName = null;
            base.CloseDialog();
        }
    }
}
