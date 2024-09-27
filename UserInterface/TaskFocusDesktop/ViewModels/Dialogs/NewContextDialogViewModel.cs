using Caliburn.Micro;
using MaterialDesignThemes.Wpf;
using Nextended.Core.Extensions;
using System;
using System.Threading.Tasks;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library;
using TaskFocusUI.Library.Models;
using TaskFocusUI.Library.Utilities;

namespace TaskFocusDesktop.ViewModels.Dialogs
{
    public class NewContextDialogViewModel : DialogViewModelBase
    {
        public NewContextDialogViewModel(IEventAggregator events, IAppState appState,
                                         IWindowManager window,
                                         IDataState dataState,
                                         IDataService dataService,
                                         IDataHelper dataHelper) : base(events, appState, window, dataState, dataService, dataHelper)
        {
            NewCollectionPlaceholderText = "Context Name";
            HeaderText = "ADD NEW CONTEXT";
        }

        protected override void CloseDialog()
        {
            NewCollectionName = null;
            base.CloseDialog();
        }

        protected override async Task ProcessSubmitAction()
        {
            if (NewCollectionName.IsNullOrWhiteSpace())
            {
                IsFeedbackError = true;
                FeedbackMessage = "Context name cannot be empty; please try again.";
            }

            else if (!_dataHelper.IsNewContextNameUnique(NewCollectionName!))
            {
                IsFeedbackError = true;
                FeedbackMessage = "Context names must be unique; please try again.";
            }
            else
            {
                IsFeedbackError = false;
                FeedbackMessage = null;

                var newContext = new ContextModel { ContextName = NewCollectionName! };
                await _dataService.AddContext(newContext);

                FeedbackMessage = "Context added!";
                await Task.Delay(TimeSpan.FromSeconds(_successMsgDisplaySec));

                // close dialog
                DialogHost.Close(_dialogIdentifier);
                FeedbackMessage = null;
                NewCollectionName = null;
            }
        }
    }
}
