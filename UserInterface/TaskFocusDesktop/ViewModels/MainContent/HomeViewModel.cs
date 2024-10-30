using Caliburn.Micro;
using Nextended.Core.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using TaskFocusDesktop.Commands;
using TaskFocusDesktop.EventModels;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Models;
using Windows.UI.Composition;

namespace TaskFocusDesktop.ViewModels.MainContent
{
    public class HomeViewModel : ViewModelBase, IHandle<LoginNotifyEvent>
    {
        protected IUserEndpoint _userEndpoint;
        protected const double _alertMsgDisplaySec = 4.0;
        protected bool _loading = false;

        private bool _showPleaseWaitLoginMessage = false;
        public bool ShowPleaseWaitLoginMessage
        {
            get { return _showPleaseWaitLoginMessage; }
            set
            {
                _showPleaseWaitLoginMessage = value;
                NotifyOfPropertyChange(() => ShowPleaseWaitLoginMessage);
            }
        }

        private bool _showNotAuthenticatedMessage;
        public bool ShowNotAuthenticatedMessage
        {
            get { return _showNotAuthenticatedMessage; }
            set 
            { 
                _showNotAuthenticatedMessage = value; 
                NotifyOfPropertyChange(() => ShowNotAuthenticatedMessage);
            }
        }

        private bool _showForgotPasswordToggle;  
        public bool ShowForgotPasswordToggle
        {
            get { return _showForgotPasswordToggle; }
            set 
            { 
                _showForgotPasswordToggle = value;
                NotifyOfPropertyChange(() => ShowForgotPasswordToggle);          
            }
        }

        private bool _showForgotPasswordInput;
        public bool ShowForgotPasswordInput
        {
            get { return _showForgotPasswordInput; }
            set 
            {
                _showForgotPasswordInput = value;
                NotifyOfPropertyChange(() => ShowForgotPasswordInput);
            }
        }

        private string? _forgotPasswordEmailAddress;
        public string? ForgotPasswordEmailAddress
        {
            get { return _forgotPasswordEmailAddress; }
            set 
            { 
                _forgotPasswordEmailAddress = value;
                NotifyOfPropertyChange(()=> ForgotPasswordEmailAddress);
            }
        }

        private string _feedbackMessage = string.Empty;
        public string FeedbackMessage
        {
            get { return _feedbackMessage; }
            set {
                _feedbackMessage = value; 
                NotifyOfPropertyChange(() => FeedbackMessage);
            }
        }

        private bool _isFeedbackError;
        public bool IsFeedbackError
        {
            get { return _isFeedbackError; }
            set 
            {
                _isFeedbackError = value; 
                NotifyOfPropertyChange(() => IsFeedbackError);
            }
        }


        public HomeViewModel(IEventAggregator events, IAppState appState, IUserEndpoint userEndpoint) : base(events, appState)
        {
            _events = events;
            _events.SubscribeOnPublishedThread(this);
            _userEndpoint = userEndpoint;

            ShowNotAuthenticatedMessage = !_appState.IsAuthenticated;
            ShowForgotPasswordToggle = !_appState.IsAuthenticated;
        }

        public ICommand ToggleShowForgotPasswordInput => new RelayCommand(execute => ShowForgotPasswordInput = !ShowForgotPasswordInput);
        public ICommand RequestSubmitForgotPasswordForm => new RelayCommand(async execute => await SubmitForgotPasswordForm());

        Task IHandle<LoginNotifyEvent>.HandleAsync(LoginNotifyEvent message, CancellationToken cancellationToken)
        {
            ShowPleaseWaitLoginMessage = true;
            ShowForgotPasswordToggle = false;
            ShowForgotPasswordInput = false;
            return Task.CompletedTask;
        }

        public static bool IsValidEmailAddress(string emailAddress)
        {
            string pattern = @"^\s*[\w\-\+_']+(\.[\w\-\+_']+)*\@[A-Za-z0-9]([\w\.-]*[A-Za-z0-9])?\.[A-Za-z][A-Za-z\.]*[A-Za-z]$";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);

            return regex.IsMatch(emailAddress);
        }

        private async Task SubmitForgotPasswordForm()
        {
            IsFeedbackError = false;

            if (ForgotPasswordEmailAddress.IsNullOrWhiteSpace())
            {
                IsFeedbackError = true;
                FeedbackMessage = "Email address cannot be empty; please try again.";
            }
            else if (!IsValidEmailAddress(ForgotPasswordEmailAddress!))
            {
                IsFeedbackError = true;
                FeedbackMessage = "Please enter a valid email address.";
            }
            else
            {
                UserModel userModel = new() { Email = ForgotPasswordEmailAddress! };
                bool validUser = await _userEndpoint.CheckUserExists(userModel);
                if (!validUser)
                {
                    IsFeedbackError = true;
                    FeedbackMessage = "No matching user found.";
                    await Task.Delay(TimeSpan.FromSeconds(_alertMsgDisplaySec));
                    FeedbackMessage = string.Empty;
                }
                else
                {
                    FeedbackMessage = "Matching user found. Processing...";
                    await Task.Delay(TimeSpan.FromSeconds(_alertMsgDisplaySec));
                    FeedbackMessage = string.Empty;
                    await SendPasswordResetEmail();
                }
            }
        }

        private async Task SendPasswordResetEmail()
        {
            try
            {
                _loading = true; // indicate request processing
                UserModel userModel = new() { Email = ForgotPasswordEmailAddress! };
                await _userEndpoint.SendPasswordResetEmail(userModel);
                _loading = false;

                IsFeedbackError = false;
                FeedbackMessage = "Password reset email sent.";
                await Task.Delay(TimeSpan.FromSeconds(_alertMsgDisplaySec));
                FeedbackMessage = string.Empty;
            }
            catch (Exception ex)
            {
                FeedbackMessage = ex.Message;
            }
        }
    }
}
