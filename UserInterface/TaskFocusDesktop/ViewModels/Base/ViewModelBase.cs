using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskFocusDesktop.ViewModels.Base
{
    public abstract class ViewModelBase : Screen
    {
        protected IEventAggregator _events;
        protected AppState _appState;

        //protected List<string> appRefreshTriggers = new List<string> {};

        public AppState.MainContentView ActiveAppStateMainContentView { get { return _appState.ActiveMainContentView; } }

        protected ViewModelBase(IEventAggregator events, AppState appState)
        {
            _events = events;
            _appState = appState;

            //_appState.AppStateChanged += AppStateChanged;
        }

        // TODO: this enum is old and any references to it need to be migrated to app state's enum instead
        protected enum ViewModelChildren
        {
            HomeVM,
            InboxVM,
            TodayVM,
            ProjectsVM,
            ContextsVM,
            CompletedVM,
            SettingsVM
        }

        protected ViewModelChildren ActiveViewModel;

        // to be defined in child components as needed
        //protected virtual bool HandleAppStateChanged(String propertyName, AppState appState)
        //{
        //    return false;
        //}

        //private void AppStateChanged(String propertyName, AppState appState)
        //{
        //    bool changesOccured = HandleAppStateChanged(propertyName, appState);
        //    //if (changesOccured)
        //    //{
        //    //    await InvokeAsync(StateHasChanged);
        //    //}
        //}
    }
}
