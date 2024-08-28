using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskFocusDesktop.Commands;
using TaskFocusDesktop.EventModels;
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

        private async Task OnSelectedContextChanged()
        {

            if (SelectedContext != null)
            {
                var focusedContextChangedEvent = new FocusedContextChangedEvent((int)SelectedContext.Id!);
                await _events.PublishOnUIThreadAsync(focusedContextChangedEvent);
            }
        }
    }
}
