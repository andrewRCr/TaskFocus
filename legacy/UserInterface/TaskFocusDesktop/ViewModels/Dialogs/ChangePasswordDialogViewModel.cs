using Caliburn.Micro;
using MaterialDesignThemes.Wpf;
using System;
using System.Text.RegularExpressions;
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
    public class ChangePasswordDialogViewModel : DialogViewModelBase
    {
        private IAPIHelper _apiHelper;
        private ILoggedInUserModel _loggedInUser;
        private CreateUserModel _authUserModel = new();

        public ChangePasswordDialogViewModel(IEventAggregator events,
                                             IAppState appState,
                                             IWindowManager window,
                                             IDataState dataState,
                                             IDataService dataService,
                                             IDataHelper dataHelper,
                                             IAPIHelper apiHelper, 
                                             ILoggedInUserModel loggedInUser) : base(events, appState, window, dataState, dataService, dataHelper)
        {
            _apiHelper = apiHelper;
            _loggedInUser = loggedInUser;
            HeaderText = "CHANGE PASSWORD";
        }

        private string _newPassword = string.Empty;
        public string NewPassword
        {
            get { return _newPassword; }
            set
            {
                _newPassword = value;
                NotifyOfPropertyChange(() => NewPassword);
            }
        }

        private string _confirmNewPassword = string.Empty;
        public string ConfirmNewPassword
        {
            get { return _confirmNewPassword; }
            set
            {
                _confirmNewPassword = value;
                NotifyOfPropertyChange(() => ConfirmNewPassword);
            }
        }

        private string? PasswordStrength(string pw)
        {
            if (string.IsNullOrWhiteSpace(pw))
            {
                return "Password is required.";
            }
            if (pw.Length < 6)
                return "Password must be at least of length 6.";
            if (!Regex.IsMatch(pw, @"[A-Z]"))
                return "Password must contain at least one capital letter.";
            if (!Regex.IsMatch(pw, @"[a-z]"))
                return "Password must contain at least one lowercase letter.";
            if (!Regex.IsMatch(pw, @"[0-9]"))
                return "Password must contain at least one digit.";
            if (!Regex.IsMatch(pw, @"[-+_!@#$%^&*.,?]"))
                return "Password must contain at least one special character.";

            return null;
        }

        private string? PasswordMatch(string arg)
        {
            if (string.IsNullOrWhiteSpace(arg))
                return "Password confirmation is required.";
            else if (NewPassword != arg)
                return "Passwords do not match.";
            return null;
        }

        protected override void CloseDialog()
        {
            NewPassword = string.Empty;
            base.CloseDialog();
        }

        protected override async Task ProcessSubmitAction()
        {
            // validate
            if (PasswordStrength(NewPassword) != null)
            {
                IsFeedbackError = true;
                FeedbackMessage = PasswordStrength(NewPassword);
            }
            else if (PasswordMatch(ConfirmNewPassword) != null)
            {
                IsFeedbackError = true;
                FeedbackMessage = PasswordMatch(ConfirmNewPassword);
            }
            else
            {
                try
                {
                    _authUserModel.FirstName = _loggedInUser.FirstName;
                    _authUserModel.LastName = _loggedInUser.LastName;
                    _authUserModel.Email = _loggedInUser.Email;

                    // add new pw to model for update
                    _authUserModel.Password = NewPassword;
                    _authUserModel.ConfirmPassword = ConfirmNewPassword;
                    await _dataService.UpdatePassword(_authUserModel);

                    CheckPasswordModel checkPasswordModel = new() { Email = _authUserModel.Email, Password = _authUserModel.Password };
                    bool success = await _dataService.CheckPasswordValid(checkPasswordModel);
                    if (success)
                    {
                        // update auth state: log user out
                        _apiHelper.LogOutUser();
                        _loggedInUser!.ResetUserModel();

                        // raise logout event for LoginWidget to handle 
                        await _events.PublishOnUIThreadAsync(new LogoutNotifyEvent());
                        // raise auth status changed event for ShellView to handle
                        await _events.PublishOnUIThreadAsync(new AuthStatusChangedEvent(false));

                        FeedbackMessage = "Password updated!";
                        await Task.Delay(TimeSpan.FromSeconds(_successMsgDisplaySec));

                        // set Alert notification on home page + navigate to home
                        await RequestMainContentViewSwitch(Utilities.ViewCatalog.MainContentView.Home);
                        await _events.PublishOnUIThreadAsync(new PasswordUpdatedNotifyEvent());

                        // close dialog
                        DialogHost.Close(_dialogIdentifier);
                        FeedbackMessage = null;
                        IsFeedbackError = false;
                        NewPassword = string.Empty;
                        ConfirmNewPassword = string.Empty;

                        // send email confirmation 
                        UserModel user = new UserModel { Email = _authUserModel.Email };
                        await _dataService.SendPasswordChangeSuccessEmail(user);
                    }
                }
                catch (Exception ex)
                {
                    FeedbackMessage = ex.Message;
                }

                if (DialogHost.IsDialogOpen(_dialogIdentifier))
                { 
                    await Task.Delay(TimeSpan.FromSeconds(_successMsgDisplaySec));
                    // close dialog
                    DialogHost.Close(_dialogIdentifier);
                    FeedbackMessage = null;
                    IsFeedbackError = false;
                    NewPassword = string.Empty;
                    ConfirmNewPassword = string.Empty;
                }
            }
        }
    }
}
