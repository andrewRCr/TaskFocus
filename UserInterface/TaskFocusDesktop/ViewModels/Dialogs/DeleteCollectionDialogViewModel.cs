using Caliburn.Micro;
using MaterialDesignThemes.Wpf;
using System;
using System.Threading.Tasks;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library;
using TaskFocusUI.Library.Utilities;

namespace TaskFocusDesktop.ViewModels.Dialogs
{
    public class DeleteCollectionDialogViewModel : DialogViewModelBase
    {
        private bool _isProjectCollection;   
        private int _focusedCollectionId;

        private string? _focusedCollectionName;
        public string? FocusedCollectionName
        {
            get { return _focusedCollectionName; }
            set
            { 
                _focusedCollectionName = value; 
                NotifyOfPropertyChange(() => FocusedCollectionName);
            }
        }

        private string? _collectionTypeStr;
        public string? CollectionTypeStr
        {
            get { return _collectionTypeStr; }
            set 
            { 
                _collectionTypeStr = value; 
                NotifyOfPropertyChange(() => CollectionTypeStr);
            }
        }


        public DeleteCollectionDialogViewModel(IEventAggregator events, IAppState appState, IWindowManager window, IDataState dataState,
                                               IDataService dataService, IDataHelper dataHelper, bool isProjectCollection,
                                               int focusedCollectionId, string focusedCollectionName) : base(events, appState, window, dataState, dataService, dataHelper)
        {
            _isProjectCollection = isProjectCollection;
            _focusedCollectionId = focusedCollectionId;
            FocusedCollectionName = focusedCollectionName;
            HeaderText = _isProjectCollection ? "DELETE PROJECT" : "DELETE CONTEXT";
            CollectionTypeStr = isProjectCollection ? "project  " : "context  ";
        }

        protected override async Task ProcessSubmitAction()
        {
            if (_isProjectCollection)
            {
                await _dataService.FetchRemoteProjectAndTasksById(_focusedCollectionId);
                SelectedProject = _dataHelper.FocusedProject;

                if (SelectedProject == null) { return; }

                IsFeedbackError = false;
                FeedbackMessage = null;

                await _dataService.DeleteProject(SelectedProject);

                FeedbackMessage = "Project deleted!";
                await Task.Delay(TimeSpan.FromSeconds(_successMsgDisplaySec));

                // close dialog
                DialogHost.Close(_dialogIdentifier);
                FeedbackMessage = null;   
            }
            else
            {
                await _dataService.FetchRemoteContextAndTasksById(_focusedCollectionId);
                SelectedContext = _dataHelper.FocusedContext;

                if (SelectedContext == null) { return; }

                IsFeedbackError = false;
                FeedbackMessage = null;

                await _dataService.DeleteContext(SelectedContext);

                FeedbackMessage = "Context deleted!";
                await Task.Delay(TimeSpan.FromSeconds(_successMsgDisplaySec));

                // close dialog
                DialogHost.Close(_dialogIdentifier);
                FeedbackMessage = null;              
            }
        }
    }
}
