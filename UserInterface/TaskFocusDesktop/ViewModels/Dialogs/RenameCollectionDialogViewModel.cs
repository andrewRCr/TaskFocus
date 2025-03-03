using Caliburn.Micro;
using MaterialDesignThemes.Wpf;
using System;
using System.Threading.Tasks;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library;
using TaskFocusUI.Library.Utilities;

namespace TaskFocusDesktop.ViewModels.Dialogs
{
    public class RenameCollectionDialogViewModel : DialogViewModelBase
    {
        private bool _isProjectCollection; 
        private int _focusedCollectionId;

        private PackIconKind _headerIconKind;
        public PackIconKind HeaderIconKind
        {
            get { return _headerIconKind; }
            set 
            { 
                _headerIconKind = value; 
                NotifyOfPropertyChange(() => HeaderIconKind);
            }
        }


        public RenameCollectionDialogViewModel(IEventAggregator events,
                                               IAppState appState,
                                               IWindowManager window,
                                               IDataState dataState,
                                               IDataService dataService,
                                               IDataHelper dataHelper,
                                               bool isProjectCollection,
                                               int focusedCollectionId,
                                               string? focusedCollectionName) : base(events, appState, window, dataState, dataService, dataHelper)
        {
            _isProjectCollection = isProjectCollection;
            _focusedCollectionId = focusedCollectionId;
            if (focusedCollectionName != null) { UpdatedCollectionName = focusedCollectionName; }
            HeaderText = _isProjectCollection ? "RENAME PROJECT" : "RENAME CONTEXT";
            HeaderIconKind = _isProjectCollection ? PackIconKind.ClipboardText : PackIconKind.Animation;
        }

        protected override void CloseDialog()
        {
            UpdatedCollectionName = null;
            base.CloseDialog();
        }

        protected override async Task ProcessSubmitAction()
        {
            if (_isProjectCollection)
            {
                await _dataService.FetchRemoteProjectAndTasksById(_focusedCollectionId);
                SelectedProject = _dataHelper.FocusedProject;

                if (SelectedProject == null) { return; }

                if (string.IsNullOrWhiteSpace(UpdatedCollectionName))
                {
                    IsFeedbackError = true;
                    FeedbackMessage = "Project name cannot be empty; please try again.";
                }
                else if (!_dataHelper.IsNewProjectNameUnique(UpdatedCollectionName!))
                {
                    IsFeedbackError = true;
                    FeedbackMessage = "Project names must be unique; please try again.";
                }
                else
                {
                    IsFeedbackError = false;
                    FeedbackMessage = null;

                    SelectedProject.ProjectName = UpdatedCollectionName!;
                    await _dataService.UpdateProjectData(SelectedProject);

                    FeedbackMessage = "Project renamed!";
                    await Task.Delay(TimeSpan.FromSeconds(_successMsgDisplaySec));

                    // close dialog
                    DialogHost.Close(_dialogIdentifier);
                    FeedbackMessage = null;
                    UpdatedCollectionName = null;
                }
            }
            else
            {
                await _dataService.FetchRemoteContextAndTasksById(_focusedCollectionId);
                SelectedContext = _dataHelper.FocusedContext;

                if (SelectedContext == null) { return; }

                if (string.IsNullOrWhiteSpace(UpdatedCollectionName))
                {
                    IsFeedbackError = true;
                    FeedbackMessage = "Context name cannot be empty; please try again.";
                }
                else if (!_dataHelper.IsNewContextNameUnique(UpdatedCollectionName!))
                {
                    IsFeedbackError = true;
                    FeedbackMessage = "Context names must be unique; please try again.";
                }
                else
                {
                    IsFeedbackError = false;
                    FeedbackMessage = null;

                    SelectedContext.ContextName = UpdatedCollectionName!;
                    await _dataService.UpdateContextData(SelectedContext);

                    FeedbackMessage = "Context renamed!";
                    await Task.Delay(TimeSpan.FromSeconds(_successMsgDisplaySec));

                    // close dialog
                    DialogHost.Close(_dialogIdentifier);
                    FeedbackMessage = null;
                    UpdatedCollectionName = null;
                }
            }
        }
    }
}
