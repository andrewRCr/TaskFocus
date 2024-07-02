using Caliburn.Micro;
using System.ComponentModel;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library;
using TaskFocusUI.Library.Utilities;

namespace TaskFocusDesktop.ViewModels.MainContent
{
    public class TodayViewModel : TaskViewModelBase, INotifyPropertyChanged
    {
        public TodayViewModel(IDataState dataState,
                      IDataService dataService,
                      IDataHelper dataHelper,
                      IEventAggregator events,
                      IWindowManager window) : base(dataState, dataService, dataHelper, events, window)
        {
        }
    }
}
