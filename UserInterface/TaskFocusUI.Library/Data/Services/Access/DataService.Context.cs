using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaskFocusUI.Library.Data.Services.Access;
using TaskFocusUI.Library.Data.State;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Data.Services
{
    public partial class DataService
    {
        private int _contextUpdateEntered = 0;
        private ContextDisplayModel? _contextBeingUpdated;

        // helper methods
        // ====================

        // data state CRUD operations
        // ====================

        // for front-end access to data state
        public List<ContextDisplayModel>? GetDataStateContexts() => _dataState.GetWorkingContexts();

        // for populating local data state
        public async Task FetchRemoteContextData()
        {
            var contextList = await _contextEndpoint.GetAllContextsForUser();
            contextList.Sort((a, b) => Nullable.Compare(a.OrderIndex, b.OrderIndex));

            var displayContextList = _mapper.Map<List<ContextDisplayModel>>(contextList);
            _dataState.SetContexts(displayContextList);

            List<ContextDisplayModel> workingDisplayContextList = displayContextList.ConvertAll(context => context.Clone());
            _dataState.SetWorkingContexts(workingDisplayContextList);

            _dataState.InvokeDataStateChanged("Contexts");
        }

        public async Task FetchRemoteContextAndTasksById(int id)
        {
            var context = await _contextEndpoint.GetContextById(id);
            var displayContext = _mapper.Map<ContextDisplayModel>(context);
            _dataHelper.FocusedContext = displayContext;

            var contextTasks = await _taskEndpoint.GetAllContextTasksById(id);
            var displayContextTasks = _mapper.Map<List<TaskDisplayModel>>(contextTasks);
            _dataHelper.FocusedContextTasks = displayContextTasks;
        }

        // TODO: needs updating, modeled after Task equivalent
        public async Task AddContext(ContextModel newContext)
        {
            if (string.IsNullOrWhiteSpace(newContext.ContextName)) { return; }

            if (!_dataHelper.IsNewContextNameUnique(newContext.ContextName))
            {
                LogError("Unable to create context: context names must be unique.");
                return;
            }

            // determine OrderIndex for context
            //newContext.OrderIndex = _dataState.Contexts!.Count > 0 ? _dataState.Contexts.Count : 0;
            newContext.OrderIndex = _dataState.GetContexts()!.Count;

            await _contextEndpoint.AddContext(newContext, _apiHelper.GetLoggedInUserId());

            // refresh Tasks, Projects, Contexts + clear NewTask
            await FetchRemoteTaskData();
            await FetchRemoteProjectData();
            await FetchRemoteContextData();
        }

        // TODO: needs updating, modeled after Task equivalent
        public async Task DeleteContext(ContextDisplayModel displayContext)
        {
            // map from ContextDisplayModel to ContextModel
            ContextModel context = _mapper.Map<ContextModel>(displayContext);

            // ensure other contexts have updated OrderIndex values
            this.ShiftCollectionOrderIndices(displayContext, _dataState.GetContexts()!);

            // handle context's tasks - remove assigned context
            List<TaskDisplayModel> contextTasks = _dataState.GetTasks()!.Where(x => x.ContextId == context.Id).ToList();
            foreach (TaskDisplayModel task in contextTasks)
            {
                task.ContextName = null;
                await UpdateTaskData(task);
            }

            await _contextEndpoint.DeleteContext(context);

            // refresh all data
            await FetchRemoteTaskData();
            await FetchRemoteProjectData();
            await FetchRemoteContextData();
        }

        // TODO: needs updating, modeled after Task equivalent
        // validates request, performs additional processing, flags for sync, refreshes UI
        public async Task UpdateContextData(ContextDisplayModel displayContext)
        {
            // map from ContextDisplayModel to ContextModel
            ContextModel context = _mapper.Map<ContextModel>(displayContext);

            if (_dataHelper.HasContextDataChanged(displayContext))
            {
                if (!_dataHelper.IsUpdatedContextNameUnique(context))
                {
                    LogError("Unable to update context: context names must be unique.");
                    return;
                }

                if (_contextBeingUpdated != null && context.Id != _contextBeingUpdated.Id)
                {
                    // unlock
                    Interlocked.Exchange(ref _contextUpdateEntered, 0);
                    _contextBeingUpdated = null;
                }

                // lock
                if (Interlocked.Increment(ref _contextUpdateEntered) != 1) { return; }
                _contextBeingUpdated = displayContext;

                // update + refresh
                await _contextEndpoint.UpdateContext(context);
                await FetchAllRemoteData();

                // unlock
                Interlocked.Exchange(ref _contextUpdateEntered, 0);
                _contextBeingUpdated = null;
            }
        }

        // alternate update method - updates entire context collection prior to refreshing UI
        //public async Task UpdateContextsOrderingIndices(List<ContextDisplayModel> displayContexts)
        //{
        //    foreach (ContextDisplayModel displayContext in displayContexts)
        //    {
        //        // only update if changed
        //        if (_dataHelper.HasContextDataChanged(displayContext))
        //        {
        //            ContextModel context = _mapper.Map<ContextModel>(displayContext);
        //            await _contextEndpoint.UpdateContext(context);
        //        }
        //    }
        //}
    }
}
