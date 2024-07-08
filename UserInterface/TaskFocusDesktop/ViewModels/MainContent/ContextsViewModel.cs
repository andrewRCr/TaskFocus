using AutoMapper;
using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Utilities;

namespace TaskFocusDesktop.ViewModels.MainContent
{
    public class ContextsViewModel : TaskViewModelBase, INotifyPropertyChanged
    {
        public ContextsViewModel(IEventAggregator events,
                                 IWindowManager window,
                                 IDataState dataState,
                                 IDataService dataService,
                                 IDataHelper dataHelper) : base(events, window, dataState, dataService, dataHelper)
        {
            OrderingIndex = "ContextIndex";
        }

        private BindingList<string>? _contextNames;
        public BindingList<string>? ContextNames
        {
            get { return _contextNames; }
            set
            {
                _contextNames = value;
                NotifyOfPropertyChange(() => ContextNames);
            }
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            if (IsLocalDataLoaded())
            {
                ContextNames = new BindingList<string>();

                foreach (var item in LocalContexts!)
                {
                    ContextNames!.Add(item.ContextName);
                }

                NotifyOfPropertyChange(() => ContextNames);
            }
        }
    }
}