using Caliburn.Micro;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using TaskFocusDesktop.Commands;
using TaskFocusDesktop.EventModels;
using TaskFocusDesktop.Utilities;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library.Data.Services.Access;
using TaskFocusUI.Library.Data.State;
using TaskFocusUI.Library.Data.Utilities;
using TaskFocusUI.Library.Models;

namespace TaskFocusDesktop.ViewModels.SidePanel
{
    public class ContextSubNavMenuViewModel : TaskViewModelBase, INotifyPropertyChanged
    {
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
            if (_dataService.IsDataStateLoaded())
            {
                LocalContexts = new ObservableCollection<ContextDisplayModel>(_dataService.GetDataStateContexts()!.OrderBy(x => x.OrderIndex).ToList());
                SubscribeToContextPropertyChangedEvents(LocalContexts);

                ContextCount = LocalContexts.Count;
                UpdateScrollHeight(AppWindowHeight);
            }
        }

        // saves updated context data to local data state on property change
        protected override async void OnExistingContextPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            await VerifyAuthAndRedirectIfExpired();

            string? changedProperty = e.PropertyName;
            ContextDisplayModel senderContext = (ContextDisplayModel)sender;
            //_logger.Info($"{senderContext.ContextName}'s property {changedProperty} was changed.");

            if (!_dataService.IsContextCurrentlyBeingUpdated(senderContext))
            {
                if (changedProperty!.Contains("Index") && !CanUpdateOrderingIndices) return;
                _dataService.UpdateContextData(senderContext);
            }
        }

        protected override bool HandleDataStateChanged(string propertyName, IDataState dataState)
        {
            if (!dataRefreshTriggers.Contains(propertyName) || ActiveMainContentView != Utilities.ViewCatalog.MainContentView.Contexts)
            {
                return false;
            }

            LoadAllLocalData();
            //_logger.Info("ContextsSubNavMenuViewModel: returned true on HandleDataStateChanged!");
            return true;
        }

        // updates context listbox and containing scrollviewer height values dynamically
        protected override void UpdateScrollHeight(int appWindowHeight)
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
    }
}
