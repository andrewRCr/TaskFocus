using Caliburn.Micro;
using MaterialDesignThemes.Wpf;
using Nextended.Core.Extensions;
using System;
using System.Threading.Tasks;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Models;
using TaskFocusUI.Library.Utilities;

namespace TaskFocusDesktop.ViewModels.Dialogs
{
    public class UpdateEmailDialogViewModel : DialogViewModelBase
    {
        private IUserEndpoint _userEndpoint;
        private UserModel _userModel = new();

        private string? _updatedEmailAddress;
        public string? UpdatedEmailAddress
        {
            get { return _updatedEmailAddress; }
            set
            {
                _updatedEmailAddress = value;
                NotifyOfPropertyChange(() => UpdatedEmailAddress);
            }
        }

        public UpdateEmailDialogViewModel(IEventAggregator events,
                                          IAppState appState,
                                          IWindowManager window,
                                          IDataState dataState,
                                          IDataService dataService,
                                          IDataHelper dataHelper,
                                          IUserEndpoint userEndpoint) : base(events, appState, window, dataState, dataService, dataHelper)
        {
            _userEndpoint = userEndpoint;
            HeaderText = "UPDATE EMAIL ADDRESS";
            UpdatedEmailAddress = _dataState.CurrentUser!.Email;
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            LoadLocalUserData();
        }

        protected void LoadLocalUserData()
        {
            if (_dataState.IsDataLoaded() && _dataState.CurrentUser != null)
            {
                _userModel.Id = _dataState.CurrentUser.Id;
                _userModel.FirstName = _dataState.CurrentUser.FirstName;
                _userModel.LastName = _dataState.CurrentUser.LastName;
                _userModel.Email = _dataState.CurrentUser.Email;
            }
        }

        protected override void CloseDialog()
        {
            UpdatedEmailAddress = null;
            base.CloseDialog();
        }

        protected override async Task ProcessSubmitAction()
        {
            if (UpdatedEmailAddress.IsNullOrWhiteSpace())
            {
                IsFeedbackError = true;
                FeedbackMessage = "Email address cannot be empty; please try again.";
            }

            _userModel.Email = UpdatedEmailAddress!;
            bool emailTaken = await _userEndpoint.CheckUserExists(_userModel);
            if (emailTaken)
            {
                IsFeedbackError = true;
                FeedbackMessage = "Email address already associated with an existing account.";
            }
            else
            {

                try
                {
                    //await _userEndpoint.RequestUpdateEmail(_userModel);
                    //NavManager.NavigateTo($"/unconfirmedupdatedemail?email={_userModel.Email}");
                }
                catch (Exception ex)
                {
                    FeedbackMessage = ex.Message;
                }

                FeedbackMessage = "Email address updated!";
                await Task.Delay(TimeSpan.FromSeconds(_successMsgDisplaySec));

                // close dialog
                DialogHost.Close(_dialogIdentifier);
                FeedbackMessage = null;
                IsFeedbackError = false;
                UpdatedEmailAddress = null;
            }
        }
    }
}
