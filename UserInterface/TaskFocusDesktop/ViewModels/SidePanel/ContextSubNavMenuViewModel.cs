using Caliburn.Micro;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TaskFocusDesktop.Commands;
using TaskFocusDesktop.EventModels;
using TaskFocusDesktop.Utilities;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library;
using TaskFocusUI.Library.Models;
using TaskFocusUI.Library.Utilities;

namespace TaskFocusDesktop.ViewModels.SidePanel
{
    public class ContextSubNavMenuViewModel : TaskViewModelBase, IHandle<AppWindowHeightChangedEvent>
    {
        private int _contextCount;
        public int ContextCount
        {
            get { return _contextCount; }
            set
            {
                _contextCount = value;
                NotifyOfPropertyChange(() => ContextCount);
            }
        }

        private int _maxSubNavMenuHeight;
        public int MaxSubNavMenuHeight
        {
            get { return _maxSubNavMenuHeight; }
            set
            {
                _maxSubNavMenuHeight = value;
                NotifyOfPropertyChange(() => MaxSubNavMenuHeight);
            }
        }

        private int _listBoxHeight;
        public int ListBoxHeight
        {
            get { return _listBoxHeight; }
            set
            {
                _listBoxHeight = value;
                NotifyOfPropertyChange(() => ListBoxHeight);
            }
        }

        private int _appWindowHeight;
        public int AppWindowHeight
        {
            get { return _appWindowHeight; }
            set
            {
                _appWindowHeight = value;
                NotifyOfPropertyChange(() => AppWindowHeight);
            }
        }

        public ContextSubNavMenuViewModel(IEventAggregator events,
                                          IAppState appState,
                                          IWindowManager window,
                                          IDataState dataState,
                                          IDataService dataService,
                                          IDataHelper dataHelper) : base(events, appState, window, dataState, dataService, dataHelper)
        {
            AppWindowHeight = (int)appState.AppWindowHeight;
        }

        public RelayCommand SelectedContextChangedCommand => new RelayCommand(async execute => await OnSelectedContextChanged());

        public RelayCommand RequestAddNewContextDialogCommand => new RelayCommand(async execute => await RequestAddNewContextDialog());

        public RelayCommand RequestDeleteSelectedContextDialogCommand => new RelayCommand(async execute => await RequestDeleteSelectedContextDialog());

        public RelayCommand RequestRenameSelectedContextDialogCommand => new RelayCommand(async execute => await RequestRenameSelectedContextDialog());

        // updates task listbox and containing scrollviewer height values dynamically
        private void UpdateScrollHeight(int appWindowHeight)
        {
            int fixedBaseSubMenuHeight = 110;
            int fixedTotalOtherWindowElementsHeight = 300;
            int requiredContextListHeight = 36 * ContextCount;
            MaxSubNavMenuHeight = requiredContextListHeight + fixedBaseSubMenuHeight;

            if (appWindowHeight - fixedTotalOtherWindowElementsHeight < requiredContextListHeight)
            {
                int difference = requiredContextListHeight - (appWindowHeight - fixedTotalOtherWindowElementsHeight);
                ListBoxHeight = requiredContextListHeight - difference;
            }
            else
            {
                ListBoxHeight = requiredContextListHeight;
            }

            AppWindowHeight = appWindowHeight;
        }

        private async Task OnSelectedContextChanged()
        {

            if (SelectedContext != null)
            {
                var focusedContextChangedEvent = new FocusedContextChangedEvent((int)SelectedContext.Id!, SelectedContext.ContextName);
                await _events.PublishOnUIThreadAsync(focusedContextChangedEvent);
            }
        }

        private async Task RequestAddNewContextDialog()
        {
            var requestShowDialogEvent = new RequestShowDialogEvent(ViewCatalog.DialogView.AddNewContextDialog);
            await _events.PublishOnUIThreadAsync(requestShowDialogEvent);
        }

        private async Task RequestRenameSelectedContextDialog()
        {
            var requestShowDialogEvent = new RequestShowDialogEvent(ViewCatalog.DialogView.RenameContextDialog);
            await _events.PublishOnUIThreadAsync(requestShowDialogEvent);
        }

        private async Task RequestDeleteSelectedContextDialog()
        {
            var requestShowDialogEvent = new RequestShowDialogEvent(ViewCatalog.DialogView.DeleteContextDialog);
            await _events.PublishOnUIThreadAsync(requestShowDialogEvent);
        }

        protected override void LoadLocalContextData()
        {
            if (_dataState.IsDataLoaded())
            {
                LocalContexts = new ObservableCollection<ContextDisplayModel>(_dataState.Contexts!);
                foreach (ContextDisplayModel context in LocalContexts!)
                {
                    context.PropertyChanged += OnExistingContextPropertyChanged!; // subscribe to property changed event
                }
                ContextCount = LocalContexts.Count;
                UpdateScrollHeight(AppWindowHeight);
            }
        }

        protected override bool HandleDataStateChanged(string propertyName, IDataState dataState)
        {
            if (!dataRefreshTriggers.Contains(propertyName) || ActiveMainContentView != Utilities.ViewCatalog.MainContentView.Contexts)
            {
                return false;
            }

            LoadAllLocalData();
            Debug.WriteLine("ContextsSubNavMenuViewModel: returned true on HandleDataStateChanged!");
            return true;
        }

        public Task HandleAsync(AppWindowHeightChangedEvent message, CancellationToken cancellationToken)
        {
            UpdateScrollHeight((int)message.NewAppWindowHeight);
            return Task.CompletedTask;
        }
    }
}
