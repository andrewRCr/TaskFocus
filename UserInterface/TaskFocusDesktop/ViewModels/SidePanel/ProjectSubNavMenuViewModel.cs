using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TaskFocusDesktop.Commands;
using TaskFocusDesktop.EventModels;
using TaskFocusDesktop.Utilities;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library;
using TaskFocusUI.Library.Models;
using TaskFocusUI.Library.Utilities;

namespace TaskFocusDesktop.ViewModels.SidePanel
{
    public class ProjectSubNavMenuViewModel : TaskViewModelBase
    {


        public ProjectSubNavMenuViewModel(IEventAggregator events, 
                                 IWindowManager window, 
                                 IDataState dataState, 
                                 IDataService dataService, 
                                 IDataHelper dataHelper) : base(events, window, dataState, dataService, dataHelper)
        {
        }

        public RelayCommand SelectedProjectChangedCommand => new RelayCommand(async execute => await OnSelectedProjectChanged());

        private async Task OnSelectedProjectChanged()
        {

            if (SelectedProject != null) {
                var focusedProjectChangedEvent = new FocusedProjectChangedEvent((int)SelectedProject.Id!);
                await _events.PublishOnUIThreadAsync(focusedProjectChangedEvent);
            }
        }
    }
}
