using Caliburn.Micro;
using MaterialDesignThemes.Wpf;
using System;
using System.Threading.Tasks;
using TaskFocusDesktop.EventModels;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Data.Services.Access;
using TaskFocusUI.Library.Data.State;
using TaskFocusUI.Library.Models;
using TaskFocusUI.Library.Data.Utilities;

namespace TaskFocusDesktop.ViewModels.Dialogs
{
    public class UpdateEmailDialogViewModel : DialogViewModelBase
    {
        private IUserEndpoint _userEndpoint;
        private IAPIHelper _apiHelper;
        private ILoggedInUserModel _loggedInUser;
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
                                          IUserEndpoint userEndpoint,
                                          IAPIHelper apiHelper,
                                          ILoggedInUserModel loggedInUser) : base(events, appState, window, dataState, dataService, dataHelper)
        {
            _userEndpoint = userEndpoint;
            _loggedInUser = loggedInUser;
            _apiHelper = apiHelper;
            HeaderText = "UPDATE EMAIL ADDRESS";
            UpdatedEmailAddress = _dataState.CurrentUser!.Email;
        }

        protected override void CloseDialog()
        {
            UpdatedEmailAddress = null;
            base.CloseDialog();
        }

        protected override async Task ProcessSubmitAction()
        {
            if (string.IsNullOrWhiteSpace(UpdatedEmailAddress))
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
                    _userModel.Id = _loggedInUser.Id;
                    _userModel.FirstName = _loggedInUser.FirstName;
                    _userModel.LastName = _loggedInUser.LastName;

                    bool success = await _userEndpoint.RequestUpdateEmail(_userModel);

                    if (success)
                    {
                        // update auth state: log user out
                        _apiHelper.LogOutUser();
                        _loggedInUser!.ResetUserModel();

                        // raise logout event for LoginWidget to handle 
                        await _events.PublishOnUIThreadAsync(new LogoutNotifyEvent());
                        // raise auth status changed event for ShellView to handle
                        await _events.PublishOnUIThreadAsync(new AuthStatusChangedEvent(false));

                        IsFeedbackError = false;
                        FeedbackMessage = "Email address change requested.";
                        await Task.Delay(TimeSpan.FromSeconds(_successMsgDisplaySec));

                        // set Alert notification on home page + navigate to home
                        _appState.AlertMessage = "Check your email for a link to confirm your updated address.";
                        await RequestMainContentViewSwitch(Utilities.ViewCatalog.MainContentView.Home);
                        await _events.PublishOnUIThreadAsync(new UnconfirmedUpdatedEmailNotifyEvent(_userModel.Email));

                        // close dialog
                        DialogHost.Close(_dialogIdentifier);
                        FeedbackMessage = null;
                        UpdatedEmailAddress = null;
                    }
                }
                catch (Exception ex)
                {
                    FeedbackMessage = ex.Message;
                }

                if (DialogHost.IsDialogOpen(_dialogIdentifier))
                {
                    // close dialog
                    DialogHost.Close(_dialogIdentifier);
                    FeedbackMessage = null;
                    IsFeedbackError = false;
                    UpdatedEmailAddress = null;
                }
            }
        }
    }
}
