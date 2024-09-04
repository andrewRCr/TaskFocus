using Caliburn.Micro;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskFocusDesktop.Commands;
using TaskFocusUI.Library.Utilities;

namespace TaskFocusDesktop.ViewModels.Base
{
    public class DialogViewModelBase : ViewModelBase
    {
        protected IDataService _dataService;
        protected IDataHelper _dataHelper;
        protected const string _dialogIdentifier = "ShellDialogHost";

        private string? _feedbackMessage;
        public string? FeedbackMessage
        {
            get { return _feedbackMessage; }
            set 
            {
                _feedbackMessage = value; 
                NotifyOfPropertyChange(() => FeedbackMessage);
            }
        }

        private bool _isFeedbackError = false;
        public bool IsFeedbackError
        {
            get { return _isFeedbackError; }
            set 
            { 
                _isFeedbackError = value; 
                NotifyOfPropertyChange(()=> IsFeedbackError);
            }
        }

        public RelayCommand CloseDialogCommand => new RelayCommand(execute =>  CloseDialog());

        public DialogViewModelBase(IEventAggregator events, IDataService dataService, IDataHelper dataHelper) : base(events)
        {
            _events = events;
            _dataService = dataService;
            _dataHelper = dataHelper;
        }

        protected virtual void CloseDialog()
        {
            FeedbackMessage = null;
            IsFeedbackError = false;
            DialogHost.Close(_dialogIdentifier);
        }
    }
}
