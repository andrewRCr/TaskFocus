using Caliburn.Micro;
using MaterialDesignThemes.Wpf;
using System.Threading.Tasks;
using TaskFocusDesktop.Commands;
using TaskFocusUI.Library;
using TaskFocusUI.Library.Utilities;

namespace TaskFocusDesktop.ViewModels.Base
{
    public class DialogViewModelBase : TaskViewModelBase
    {
        //protected IDataService _dataService;
        //protected IDataHelper _dataHelper;
        
        protected const string _dialogIdentifier = "ShellDialogHost";

        public RelayCommand ProcessSubmitActionCommand => new RelayCommand(async execute => await ProcessSubmitAction());

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

        private string? _newCollectionPlaceholderText;
        public string? NewCollectionPlaceholderText
        {
            get { return _newCollectionPlaceholderText; }
            set 
            { 
                _newCollectionPlaceholderText = value; 
                NotifyOfPropertyChange(() => NewCollectionPlaceholderText);
            }
        }

        private string? _newCollectionName;
        public string? NewCollectionName
        {
            get { return _newCollectionName; }
            set
            {
                _newCollectionName = value;
                NotifyOfPropertyChange(() => NewCollectionName);
            }
        }

        private string? _updatedCollectionName;
        public string? UpdatedCollectionName
        {
            get { return _updatedCollectionName; }
            set 
            { 
                _updatedCollectionName = value; 
                NotifyOfPropertyChange(() => UpdatedCollectionName);
            }
        }

        private string? _currentCollectionName;
        public string? CurrentCollectionName
        {
            get { return _currentCollectionName; }
            set 
            { 
                _currentCollectionName = value; 
                NotifyOfPropertyChange(() => CurrentCollectionName);
            }
        }


        private string? _headerText;
        public string? HeaderText
        {
            get { return _headerText; }
            set
            {
                _headerText = value;
                NotifyOfPropertyChange(() => HeaderText);
            }
        }

        public RelayCommand CloseDialogCommand => new RelayCommand(execute =>  CloseDialog());

        public DialogViewModelBase(IEventAggregator events,
                              IWindowManager window,
                              IDataState dataState,
                              IDataService dataService,
                              IDataHelper dataHelper) : base(events, window, dataState, dataService, dataHelper)
        {
            _events = events;
            _window = window;
            _dataService = dataService;
            _dataHelper = dataHelper;
        }

        protected virtual void CloseDialog()
        {
            FeedbackMessage = null;
            IsFeedbackError = false;
            DialogHost.Close(_dialogIdentifier);
        }

        protected async virtual Task ProcessSubmitAction()
        {
            await Task.CompletedTask;
        }
    }
}
