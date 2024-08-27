using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
