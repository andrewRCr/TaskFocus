using Caliburn.Micro;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using TaskFocusDesktop.EventModels;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Data.Services.Access;
using TaskFocusUI.Library.Data.State;
using TaskFocusUI.Library.Data.Utilities;
using TaskFocusUI.Library.Models;

namespace TaskFocusDesktop.ViewModels.Dialogs
{
    public class UpdateEmailDialogViewModel : DialogViewModelBase
    {
        private IAPIHelper _apiHelper;
        private ILoggedInUserModel _loggedInUser;
        private UserModel _userModel = new();
        private bool _submitting;

        public UpdateEmailDialogViewModel(IEventAggregator events,
                                  IAppState appState,
                                  IWindowManager window,
                                  IDataState dataState,
                                  IDataService dataService,
                                  IDataHelper dataHelper,
                                  IAPIHelper apiHelper,
                                  ILoggedInUserModel loggedInUser) : base(events, appState, window, dataState, dataService, dataHelper)
        {
            _loggedInUser = loggedInUser;
            _apiHelper = apiHelper;
            HeaderText = "UPDATE EMAIL ADDRESS";
            UpdatedEmailAddress = _dataService.GetDataStateCurrentUser()!.Email;
        }

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

        protected override void CloseDialog()
        {
            UpdatedEmailAddress = null;
            base.CloseDialog();
        }

        protected override async Task ProcessSubmitAction()
        {
            if (_submitting)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(UpdatedEmailAddress))
            {
                IsFeedbackError = true;
                FeedbackMessage = "Email address cannot be empty; please try again.";
                return;
            }

            if (!new EmailAddressAttribute().IsValid(UpdatedEmailAddress))
            {
                IsFeedbackError = true;
                FeedbackMessage = "Please enter a valid email address.";
                return;
            }

            _submitting = true;
            try
            {
                _userModel.Email = UpdatedEmailAddress;
                if (await _dataService.CheckUserExists(_userModel))
                {
                    IsFeedbackError = true;
                    FeedbackMessage = "Email address already associated with an existing account.";
                    return;
                }
                _userModel.Id = _loggedInUser.Id;
                _userModel.FirstName = _loggedInUser.FirstName;
                _userModel.LastName = _loggedInUser.LastName;
                if (!await _dataService.RequestUpdateEmail(_userModel))
                {
                    IsFeedbackError = true;
                    FeedbackMessage = "Could not request an email address change. Please try again.";
                    return;
                }

                // update auth state: log user out
                _apiHelper.LogOutUser();
                _loggedInUser!.ResetUserModel();

                await _events.PublishOnUIThreadAsync(new LogoutNotifyEvent());
                await _events.PublishOnUIThreadAsync(new AuthStatusChangedEvent(false));

                IsFeedbackError = false;
                FeedbackMessage = "Email address change requested.";
                await Task.Delay(TimeSpan.FromSeconds(_successMsgDisplaySec));

                _appState.AlertMessage = "Check your email for a link to confirm your updated address.";
                await RequestMainContentViewSwitch(Utilities.ViewCatalog.MainContentView.Home);
                await _events.PublishOnUIThreadAsync(new UnconfirmedUpdatedEmailNotifyEvent(_userModel.Email));

                CloseDialog();
            }
            catch (Exception ex)
            {
                IsFeedbackError = true;
                FeedbackMessage = ex.Message;
            }
            finally
            {
                _submitting = false;
            }
        }
    }
}
