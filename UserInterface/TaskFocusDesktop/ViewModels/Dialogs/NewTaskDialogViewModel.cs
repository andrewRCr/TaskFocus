using Caliburn.Micro;
using MaterialDesignThemes.Wpf;
using System;
using System.Dynamic;
using System.Threading.Tasks;
using System.Windows;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library;
using TaskFocusUI.Library.Models;
using TaskFocusUI.Library.Utilities;

namespace TaskFocusDesktop.ViewModels.Dialogs
{
    public class NewTaskDialogViewModel : DialogViewModelBase
    {
        private TaskDisplayModel? _newTask;
        public TaskDisplayModel? NewTask
        {
            get { return _newTask; }
            set 
            { 
                _newTask = value;
                NotifyOfPropertyChange(() => NewTask);
            }
        }

        private bool _userHasProjects;
        public bool UserHasProjects
        { 
            get { return _userHasProjects; }
            set
            {
                _userHasProjects = value;
                NotifyOfPropertyChange(() => UserHasProjects);
            }
        }

        private bool _userHasContexts;
        public bool UserHasContexts
        {
            get { return _userHasContexts; }
            set
            {
                _userHasContexts = value;
                NotifyOfPropertyChange(() => UserHasContexts);
            }
        }

        public NewTaskDialogViewModel(IEventAggregator events,
                                      IAppState appState,
                                      IWindowManager window,
                                      IDataState dataState,
                                      IDataService dataService,
                                      IDataHelper dataHelper,
                                      string? focusedProjectName = null,
                                      string? focusedContextName = null) : base(events, appState, window, dataState, dataService, dataHelper)
        {
            HeaderText = "ADD NEW TASK";
            NewTask = new TaskDisplayModel();

            // context-awareness: if on Projects or Contexts pages and have highlighted in the UI a particular one, pre-populate field
            if (focusedProjectName != null) { NewTask.ProjectName = focusedProjectName; }
            else if (focusedContextName != null) { NewTask.ContextName = focusedContextName; }
        }

        protected override async void OnViewLoaded(object view)
        {
            try
            {
                // load projects / contexts (not tasks)
                LoadLocalProjectData();
                LoadLocalContextData();
                UserHasProjects = LocalProjects != null ? LocalProjects.Count > 0 : false;
                UserHasContexts = LocalContexts != null ? LocalContexts.Count > 0 : false;
            }
            catch (Exception ex)
            {
                dynamic settings = new ExpandoObject();
                settings.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                settings.ResizeMode = ResizeMode.NoResize;
                settings.Title = "Exception!";

                var status = IoC.Get<StatusInfoViewModel>();
                status.UpdateMessage($"{ex.Source} threw an exception:", ex.Message);
                await _window.ShowDialogAsync(status, null, settings);
                await TryCloseAsync();
            }
        }

        protected override async Task ProcessSubmitAction()
        {
            if (NewTask != null)
            {
                if (string.IsNullOrWhiteSpace(NewTask.TaskName))
                {
                    IsFeedbackError = true;
                    FeedbackMessage = "Task name cannot be empty; please try again.";
                }
                else
                {
                    IsFeedbackError = false;
                    FeedbackMessage = null;

                    await _dataService.AddTask(NewTask);

                    FeedbackMessage = "Task added!";
                    await Task.Delay(TimeSpan.FromSeconds(_successMsgDisplaySec));

                    // close dialog
                    DialogHost.Close(_dialogIdentifier);
                    FeedbackMessage = null;
                    NewTask = null;
                }
            }
        }
    }
}
