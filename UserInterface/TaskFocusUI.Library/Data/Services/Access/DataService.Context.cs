using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using TaskFocusUI.Library.Data.Services.Access;
using TaskFocusUI.Library.Data.State;
using TaskFocusUI.Library.Data.Utilities;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Data.Services
{
    public partial class DataService
    {
        private int _contextUpdateEntered = 0;
        private ContextDisplayModel? _contextBeingUpdated;

        // helper methods
        // ====================

        private async Task ProcessLocalContextUpdate(ContextDisplayModel workingContext, bool nameChanged = false)
        {
            if (nameChanged)
            {
                // handle context's tasks - update assigned context name
                List<TaskDisplayModel> contextTasks;
                if (workingContext.Id != null)
                {

                    contextTasks = _dataState.GetWorkingTasks()!.Where(x => x.ContextId == workingContext.Id).ToList();
                    foreach (TaskDisplayModel task in contextTasks)
                    {
                        task.ContextName = workingContext.ContextName;
                        UpdateTaskData(task);
                    }
                }
            }

            // update data state Contexts object from WorkingContexts copy
            workingContext.ClientLastUpdated = DateTimeOffset.Now; // flag for sync
            if (workingContext.Id == null) // context hasn't yet been inserted on server; pending push
            {
                // update standard client data state copy
                var dataStateContext= _dataState.GetContexts()!.Find(x => x.TempLocalId == workingContext.TempLocalId);
                if (dataStateContext != null) dataStateContext.ValueAssign(workingContext);

                // update changedContextData copy of task
                // note: only need to track pending property changes in changedContextData if context has never been pushed
                var queuedChangedContext = _dataState.ChangedContextData!.Find(x => x.TempLocalId == workingContext.TempLocalId);
                if (queuedChangedContext != null) queuedChangedContext.ValueAssign(workingContext);
            }
            else
            {
                // update standard client data state copy
                var dataStateContext = _dataState.GetContexts()!.Find(x => x.Id == workingContext.Id);
                if (dataStateContext != null) dataStateContext.ValueAssign(workingContext);

                // add copy to ChangedContextData
                // don't duplicate if already had another update prior to push
                var alreadyQueued = _dataState.ChangedContextData.Where(
                    x => x.Id == workingContext.Id);
                if (!alreadyQueued.Any()) { _dataState.ChangedContextData.Add(workingContext.Clone()); }
            }
        }

        // updates local "working" copy of context data, for use after sync
        private void UpdateWorkingContextsFromDataState()
        {
            List<ContextDisplayModel> workingContextList = _dataState.GetContexts()!.ConvertAll(context => context.Clone());
            _dataState.SetWorkingContexts(workingContextList);
        }

        public bool IsContextCurrentlyBeingUpdated(ContextDisplayModel context)
        {
            if (_contextBeingUpdated != null)
            {
                if (context.Id == null && context.TempLocalId != null)
                {
                    return _contextBeingUpdated.TempLocalId == context.TempLocalId;
                }
                else { return _contextBeingUpdated.Id == context.Id; }
            }

            return false;
        }

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

            _dataState.InvokeDataStateChanged(nameof(EDataRefreshType.Contexts));
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

        // validates request, processes local add, flags for sync, refreshes UI
        public ContextDisplayModel? AddContext(ContextModel newContext)
        {
            if (string.IsNullOrWhiteSpace(newContext.ContextName)) { return null; }

            if (!_dataHelper.IsNewContextNameUnique(newContext.ContextName))
            {
                LogError("Unable to create context: context names must be unique.");
                return null;
            }

            // map to display model
            ContextDisplayModel newDisplayContext = _mapper.Map<ContextDisplayModel>(newContext);
            // determine OrderIndex for context
            newDisplayContext.OrderIndex = _dataState.GetContexts()!.Count;
            // give temp local tracking id
            newDisplayContext.TempLocalId = ++_dataState.TempContextId;

            // flag for sync
            newDisplayContext.ClientLastUpdated = DateTimeOffset.Now;
            _dataState.ChangedContextData.Add(newDisplayContext.Clone());

            // update local data state
            _dataState.GetWorkingContexts()!.Add(newDisplayContext.Clone());
            _dataState.GetContexts()!.Add(newDisplayContext.Clone());

            // trigger UI update + request sync
            _dataState.InvokeDataStateChanged(nameof(EDataRefreshType.Contexts));
            InvokeSyncRequest(nameof(AddContext));

            return newDisplayContext;
        }

        // validates request, processes local deletion, flags for sync, refreshes UI
        public async Task DeleteContext(ContextDisplayModel workingContext)
        {
            // map from ContextDisplayModel to ContextModel
            ContextModel context = _mapper.Map<ContextModel>(workingContext);

            // ensure other contexts have updated OrderIndex values
            this.ShiftCollectionOrderIndices(workingContext, _dataState.GetWorkingContexts()!.ToList());
            // ^ this updates those tasks' orderIndex value, but doesn't flag for sync / do additional processing
            // that will be caught and processed in the next step when UpdateTaskData is called

            // handle context's tasks - remove assigned context
            List<TaskDisplayModel> contextTasks = _dataState.GetWorkingTasks()!.Where(x => x.ContextId == context.Id).ToList();
            foreach (TaskDisplayModel task in contextTasks)
            {
                task.ContextName = null;
                UpdateTaskData(task);
            }

            // update local data state
            if (context.Id == null) // never existed on server; insert was pending push
            {
                // local delete
                var dataStateContext = _dataState.GetContexts()!.Find(x => x.TempLocalId == workingContext.TempLocalId);
                if (dataStateContext != null) _dataState.GetContexts()!.Remove(dataStateContext);
                var changedContext = _dataState.ChangedContextData.Find(x => x.TempLocalId == workingContext.TempLocalId);
                if (changedContext != null) _dataState.ChangedContextData.Remove(changedContext);
                _dataState.GetWorkingContexts()!.Remove(workingContext);
            }
            else
            {
                var dataStateContext = _dataState.GetContexts()!.Find(x => x.Id == context.Id)!;
                // flag for server delete on sync
                dataStateContext.Deleted = DateTimeOffset.Now;
                dataStateContext.ClientLastUpdated = DateTimeOffset.Now;

                // if had an update pending push, don't add a duplicate to changedContextData
                var alreadyQueued = _dataState.ChangedContextData.Where(
                    x => x.Id == dataStateContext.Id);
                if (!alreadyQueued.Any()) _dataState.ChangedContextData.Add(dataStateContext.Clone());

                // local delete
                if (dataStateContext != null) _dataState.GetContexts()!.Remove(dataStateContext);
                _dataState.GetWorkingContexts()!.Remove(workingContext);
            }

            // trigger UI update
            _dataState.InvokeDataStateChanged(nameof(EDataRefreshType.Contexts));
        }

        // validates request, performs additional processing, flags for sync, refreshes UI
        public async Task UpdateContextData(ContextDisplayModel workingContext)
        {
            CollectionDataCompareResult compareResult = _dataHelper.HasContextDataChanged(workingContext);

            if (compareResult.HasChanged)
            {
                if (!_dataHelper.IsUpdatedContextNameUnique(workingContext))
                {
                    LogError("Unable to update context; context names must be unique.");
                    return;
                }
            }

            if (!IsContextCurrentlyBeingUpdated(workingContext))
            {
                // unlock
                Interlocked.Exchange(ref _contextUpdateEntered, 0);
                _contextBeingUpdated = null;
            }

            // lock, to prevent other property changes during processing from triggering new Update calls
            if (Interlocked.Increment(ref _contextUpdateEntered) != 1) { return; }
            _contextBeingUpdated = workingContext;

            await ProcessLocalContextUpdate(workingContext, compareResult.CollectionNameChanged);

            // unlock
            Interlocked.Exchange(ref _contextUpdateEntered, 0);
            _contextBeingUpdated = null;
            // trigger UI update
            _dataState.InvokeDataStateChanged("Contexts");
        }
    }
}
