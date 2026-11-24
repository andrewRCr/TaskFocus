using Caliburn.Micro;
using MaterialDesignThemes.Wpf;
using System;
using System.Threading.Tasks;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library.Data.Services.Access;
using TaskFocusUI.Library.Data.State;
using TaskFocusUI.Library.Data.Utilities;

namespace TaskFocusDesktop.ViewModels.Dialogs
{
    public class DeleteCollectionDialogViewModel : DialogViewModelBase
    {
        private bool _isProjectCollection;   
        private int _focusedCollectionId;

        public DeleteCollectionDialogViewModel(IEventAggregator events,
                                               IAppState appState,
                                               IWindowManager window,
                                               IDataState dataState,
                                               IDataService dataService,
                                               IDataHelper dataHelper,
                                               bool isProjectCollection,
                                               int focusedCollectionId,
                                               string focusedCollectionName) : base(events, appState, window, dataState, dataService, dataHelper)
        {
            _isProjectCollection = isProjectCollection;
            _focusedCollectionId = focusedCollectionId;
            FocusedCollectionName = focusedCollectionName;
            HeaderText = _isProjectCollection ? "DELETE PROJECT" : "DELETE CONTEXT";
            CollectionTypeStr = isProjectCollection ? "project  " : "context  ";
        }

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

        protected override async Task ProcessSubmitAction()
        {
            if (_isProjectCollection)
            {
                SelectedProject = _dataService.GetDataStateProjects()?.Find(x => x.Id == _focusedCollectionId);
                if (SelectedProject == null)
                {
                    IsFeedbackError = true;
                    FeedbackMessage = "This project is no longer available.";
                    return;
                }

                IsFeedbackError = false;
                FeedbackMessage = null;

                _dataService.DeleteProject(SelectedProject);

                FeedbackMessage = "Project deleted!";
                await Task.Delay(TimeSpan.FromSeconds(_successMsgDisplaySec));

                // close dialog
                DialogHost.Close(_dialogIdentifier);
                FeedbackMessage = null;   
            }
            else
            {
                SelectedContext = _dataService.GetDataStateContexts()?.Find(x => x.Id == _focusedCollectionId);
                if (SelectedContext == null)
                {
                    IsFeedbackError = true;
                    FeedbackMessage = "This context is no longer available.";
                    return;
                }

                IsFeedbackError = false;
                FeedbackMessage = null;

                _dataService.DeleteContext(SelectedContext);

                FeedbackMessage = "Context deleted!";
                await Task.Delay(TimeSpan.FromSeconds(_successMsgDisplaySec));

                // close dialog
                DialogHost.Close(_dialogIdentifier);
                FeedbackMessage = null;              
            }
        }
    }
}
