using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskFocusDesktop.Commands;
using TaskFocusDesktop.EventModels;
using TaskFocusDesktop.Utilities;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library;
using TaskFocusUI.Library.Utilities;

namespace TaskFocusDesktop.ViewModels.SidePanel
{
    public class ContextSubNavMenuViewModel : TaskViewModelBase
    {
        public ContextSubNavMenuViewModel(IEventAggregator events,
                                          IWindowManager window,
                                          IDataState dataState,
                                          IDataService dataService,
                                          IDataHelper dataHelper) : base(events, window, dataState, dataService, dataHelper)
        {
        }

        public RelayCommand SelectedContextChangedCommand => new RelayCommand(async execute => await OnSelectedContextChanged());

        public RelayCommand RequestAddNewContextDialogCommand => new RelayCommand(async execute => await RequestAddNewContextDialog());

        private async Task OnSelectedContextChanged()
        {

            if (SelectedContext != null)
            {
                var focusedContextChangedEvent = new FocusedContextChangedEvent((int)SelectedContext.Id!);
                await _events.PublishOnUIThreadAsync(focusedContextChangedEvent);
            }
        }

        private async Task RequestAddNewContextDialog()
        {
            var requestShowDialogEvent = new RequestShowDialogEvent(ViewCatalog.DialogView.AddNewContextDialog);
            await _events.PublishOnUIThreadAsync(requestShowDialogEvent);
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
    }
}
