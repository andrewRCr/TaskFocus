using System.Net;
using System.Net.Http.Json;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Data.Services;
using TaskFocusUI.Library.Data.Services.Synchronization;
using TaskFocusUI.Library.Data.State;
using TaskFocusUI.Library.Data.Utilities;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Tests
{
    public sealed class ContextCreationTests : IDisposable
    {
        private readonly ContextApi _api = new();
        private readonly DataState _state = new();
        private readonly IMapper _mapper;
        private readonly DataHelper _helper;
        private readonly DataService _data;
        private readonly UserEndpoint _users;
        private readonly TaskEndpoint _tasks;
        private readonly ProjectEndpoint _projects;
        private readonly ContextEndpoint _contexts;

        public ContextCreationTests()
        {
            _mapper = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<UserModel, UserDisplayModel>();
                cfg.CreateMap<UserSettingsModel, UserSettingsDisplayModel>();
                cfg.CreateMap<TaskModel, TaskDisplayModel>();
                cfg.CreateMap<ProjectModel, ProjectDisplayModel>().ReverseMap();
                cfg.CreateMap<ContextModel, ContextDisplayModel>().ReverseMap();
            }).CreateMapper();

            _helper = new DataHelper(_mapper, _state);
            _users = new UserEndpoint(_api);
            _tasks = new TaskEndpoint(_api);
            _projects = new ProjectEndpoint(_api);
            _contexts = new ContextEndpoint(_api);
            _data = new DataService(_mapper, _helper, _state, _users, _tasks, _projects, _contexts, NullLogger<DataService>.Instance);
        }

        [Fact]
        public async Task NewContextReceivesItsServerIdWithNoProjects()
        {
            await LoadAsync();
            _data.AddContext(new ContextModel { ContextName = "Errands" });
            var working = Assert.Single(_data.GetDataStateContexts()!);
            Assert.Null(working.Id);

            ContextDisplayModel? storedAtPush = null;
            _api.BeforePull = () => storedAtPush = Assert.Single(_state.GetContexts()!).Clone();
            Assert.True(await CreateSyncService().TrySync());

            var inserted = Assert.Single(_api.Contexts);
            AssertAcknowledged(inserted, storedAtPush);
            AssertAcknowledged(inserted, working);
            Assert.Empty(_data.GetDataStateProjects()!);
            var result = _data.GetDataStateLastSyncResult();
            Assert.Equal((1, 0, 0), (result.NumRowsInserted, result.NumRowsUpdated, result.NumRowsDeleted));
        }

        [Fact]
        public async Task ContextAcknowledgementLeavesTheProjectAtTheSameIndexIntact()
        {
            const int projectId = 37;
            _api.Projects.Add(new ProjectModel { Id = projectId, ProjectName = "Shopping" });
            // Supply an ID even to the old endpoint so the test isolates where it is written.
            _api.ReturnContextForLegacyPost = true;
            await LoadAsync();
            _data.AddContext(new ContextModel { ContextName = "Errands" });

            int? storedProjectId = null;
            int? workingProjectId = null;
            ContextDisplayModel? storedContext = null;
            _api.BeforePull = () =>
            {
                storedProjectId = Assert.Single(_state.GetProjects()!).Id;
                workingProjectId = Assert.Single(_data.GetDataStateProjects()!).Id;
                storedContext = Assert.Single(_state.GetContexts()!).Clone();
            };
            Assert.True(await CreateSyncService().TrySync());

            Assert.Equal(projectId, storedProjectId);
            Assert.Equal(projectId, workingProjectId);
            AssertAcknowledged(Assert.Single(_api.Contexts), storedContext);
        }

        [Fact]
        public async Task TwoPendingContextsReceiveTheirOwnServerIds()
        {
            await LoadAsync();
            _data.AddContext(new ContextModel { ContextName = "Errands" });
            _data.AddContext(new ContextModel { ContextName = "Home" });
            var working = _data.GetDataStateContexts()!;
            Assert.NotEqual(working[0].TempLocalId, working[1].TempLocalId);

            List<ContextDisplayModel>? storedAtPush = null;
            _api.BeforePull = () => storedAtPush = _state.GetContexts()!.ConvertAll(context => context.Clone());
            Assert.True(await CreateSyncService().TrySync());

            Assert.NotNull(storedAtPush);
            Assert.Equal(2, _api.Contexts.Count);
            Assert.Collection(storedAtPush,
                context => AssertAcknowledged(_api.Contexts[0], context),
                context => AssertAcknowledged(_api.Contexts[1], context));
            Assert.Collection(working,
                context => AssertAcknowledged(_api.Contexts[0], context),
                context => AssertAcknowledged(_api.Contexts[1], context));
            Assert.NotEqual(working[0].Id, working[1].Id);
        }

        [Fact]
        public async Task AddingContextAdvancesOnlyTheContextTemporaryIdCounter()
        {
            await LoadAsync();
            _state.SetTempProjectId(7);
            _state.SetTempContextId(3);

            var context = _data.AddContext(new ContextModel { ContextName = "Errands" });

            Assert.NotNull(context);
            Assert.Equal(7, _state.GetTempProjectId());
            Assert.Equal(4, _state.GetTempContextId());
            Assert.Equal(4, context.TempLocalId);
        }

        [Fact]
        public async Task FirstRenameAtAcknowledgementIsPushedOnNextSync()
        {
            await LoadAsync();
            _data.AddContext(new ContextModel { ContextName = "Errands" });
            var working = Assert.Single(_data.GetDataStateContexts()!);
            ContextDisplayModel? storedAtPush = null;
            _api.BeforePull = () =>
            {
                storedAtPush = Assert.Single(_state.GetContexts()!).Clone();
                working.ContextName = "Home";
                _data.UpdateContextData(working);
            };

            var sync = CreateSyncService();
            Assert.True(await sync.TrySync());
            var inserted = Assert.Single(_api.Contexts);
            AssertAcknowledged(inserted, storedAtPush);
            Assert.Equal("Errands", inserted.ContextName);

            Assert.True(await sync.TrySync());

            Assert.Equal("Home", Assert.Single(_api.Contexts).ContextName);
        }

        [Fact]
        public async Task DeletingContextWhilePostIsPendingRemovesTheInsertedRow()
        {
            await LoadAsync();
            _data.AddContext(new ContextModel { ContextName = "Errands" });

            var sync = await SyncWithPendingContextPost(() => _data.DeleteContext(Assert.Single(_data.GetDataStateContexts()!)));
            var pendingDelete = Assert.Single(_state.GetChangedContextData());
            Assert.Equal(Assert.Single(_api.Contexts).Id, pendingDelete.Id);
            Assert.Null(pendingDelete.TempLocalId);
            Assert.NotNull(pendingDelete.Deleted);
            Assert.Empty(_state.GetContexts()!);
            Assert.Empty(_data.GetDataStateContexts()!);
            AssertSingleContextInsert();

            Assert.True(await sync.TrySync());
            Assert.Empty(_api.Contexts);
            AssertLocalContextsMatchServer();
            AssertContextSyncCounts(0, 0, 1);
        }

        [Fact]
        public async Task DeletingUnsentContextFreesItsNameForAnotherAdd()
        {
            await LoadAsync();
            _data.AddContext(new ContextModel { ContextName = "Errands" });
            _data.DeleteContext(Assert.Single(_data.GetDataStateContexts()!));

            Assert.NotNull(_data.AddContext(new ContextModel { ContextName = "Errands" }));
            Assert.Equal("Errands", Assert.Single(_data.GetDataStateContexts()!).ContextName);
        }

        [Fact]
        public async Task DeletingPendingProjectLeavesPendingContextEditable()
        {
            await LoadAsync();
            _api.BeforePull = () =>
            {
                _data.AddContext(new ContextModel { ContextName = "Errands" });
                _data.AddProject(new ProjectModel { ProjectName = "Shopping" });
                _data.DeleteProject(Assert.Single(_data.GetDataStateProjects()!));

                var context = Assert.Single(_data.GetDataStateContexts()!);
                context.ContextName = "Home";
                _data.UpdateContextData(context);
            };

            Assert.True(await CreateSyncService().TrySync());
            Assert.Empty(_state.GetChangedProjectData());
            Assert.Empty(_data.GetDataStateProjects()!);
            Assert.Equal("Home", Assert.Single(_state.GetChangedContextData()).ContextName);
        }

        [Fact]
        public async Task RenameDuringContextPostIsSentOnNextSync()
        {
            await LoadAsync();
            _data.AddContext(new ContextModel { ContextName = "Errands" });

            var sync = await SyncWithPendingContextPost(() => RenameContext("Errands", "Home"));
            var server = Assert.Single(_api.Contexts);
            var queued = Assert.Single(_state.GetChangedContextData());
            Assert.Equal("Home", queued.ContextName);
            Assert.Equal(server.Id, queued.Id);
            Assert.Equal(server.ServerLastUpdated, queued.ServerLastUpdated);
            Assert.Null(queued.TempLocalId);
            Assert.Equal("Home", Assert.Single(_state.GetContexts()!).ContextName);
            Assert.Equal("Home", Assert.Single(_data.GetDataStateContexts()!).ContextName);
            Assert.Equal("Errands", server.ContextName);
            Assert.Equal(0, _api.ContextUpdates);
            AssertSingleContextInsert();

            Assert.True(await sync.TrySync());
            Assert.Equal("Home", Assert.Single(_api.Contexts).ContextName);
            Assert.Equal(1, _api.ContextUpdates);
            AssertLocalContextsMatchServer();
            AssertContextSyncCounts(0, 1, 0);
        }

        [Fact]
        public async Task RenameOfAcknowledgedContextDuringAnotherPostStaysQueued()
        {
            await LoadAsync();
            _data.AddContext(new ContextModel { ContextName = "Errands" });
            _data.AddContext(new ContextModel { ContextName = "Office" });

            var sync = await SyncWithPendingContextPost(() => RenameContext("Errands", "Home"), "Office");
            var queued = Assert.Single(_state.GetChangedContextData());
            Assert.Equal(_api.Contexts[0].Id, queued.Id);
            Assert.Equal("Home", queued.ContextName);
            Assert.Equal("Errands", _api.Contexts[0].ContextName);
            AssertContextSyncCounts(2, 0, 0);

            Assert.True(await sync.TrySync());
            Assert.Equal("Home", _api.Contexts[0].ContextName);
            AssertLocalContextsMatchServer();
            AssertContextSyncCounts(0, 1, 0);
        }

        [Fact]
        public async Task DeleteOfAcknowledgedContextDuringAnotherPostStaysQueued()
        {
            await LoadAsync();
            _data.AddContext(new ContextModel { ContextName = "Errands" });
            _data.AddContext(new ContextModel { ContextName = "Office" });

            var sync = await SyncWithPendingContextPost(
                () => _data.DeleteContext(_data.GetDataStateContexts()!.Single(x => x.ContextName == "Errands")), "Office");
            var queued = Assert.Single(_state.GetChangedContextData());
            Assert.Equal(_api.Contexts[0].Id, queued.Id);
            Assert.NotNull(queued.Deleted);
            Assert.Equal("Office", Assert.Single(_state.GetContexts()!).ContextName);
            Assert.Equal("Office", Assert.Single(_data.GetDataStateContexts()!).ContextName);
            AssertContextSyncCounts(2, 0, 0);

            Assert.True(await sync.TrySync());
            Assert.Equal("Office", Assert.Single(_api.Contexts).ContextName);
            AssertLocalContextsMatchServer();
            AssertContextSyncCounts(0, 0, 1);
        }

        [Fact]
        public async Task AddingSecondContextDuringFirstPostKeepsBothContexts()
        {
            await LoadAsync();
            var first = _data.AddContext(new ContextModel { ContextName = "Errands" });
            ContextDisplayModel? second = null;

            var sync = await SyncWithPendingContextPost(
                () => second = _data.AddContext(new ContextModel { ContextName = "Office" }));
            Assert.NotNull(second);
            Assert.NotEqual(first!.TempLocalId, second.TempLocalId);
            Assert.Equal(second.TempLocalId, _state.GetTempContextId());
            var queued = Assert.Single(_state.GetChangedContextData());
            Assert.Null(queued.Id);
            Assert.Equal(second.TempLocalId, queued.TempLocalId);
            var server = Assert.Single(_api.Contexts);
            foreach (var local in new[] { _state.GetContexts()!, _data.GetDataStateContexts()! })
            {
                AssertAcknowledged(server, Assert.Single(local, x => x.ContextName == "Errands"));
                var pending = Assert.Single(local, x => x.ContextName == "Office");
                Assert.Equal(second.TempLocalId, pending.TempLocalId);
                Assert.Null(pending.Id);
            }
            AssertSingleContextInsert();

            Assert.True(await sync.TrySync());
            Assert.Equal(2, _api.Contexts.Count);
            Assert.NotEqual(_api.Contexts[0].Id, _api.Contexts[1].Id);
            Assert.Equal(0, _state.GetTempContextId());
            AssertLocalContextsMatchServer();
            AssertSingleContextInsert();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ReorderPreservesLaterRemoteRename(bool existingContext)
        {
            if (existingContext) AddExistingContext();
            await LoadAsync();

            void ReorderAndRenameRemotely()
            {
                var working = Assert.Single(_data.GetDataStateContexts()!);
                working.OrderIndex = 5;
                _data.UpdateContextData(working);
                var server = Assert.Single(_api.Contexts);
                _api.Contexts[0] = new ContextModel
                {
                    Id = server.Id, UserId = server.UserId, ContextName = "Other device", OrderIndex = server.OrderIndex,
                    ClientLastUpdated = server.ServerLastUpdated.AddSeconds(1),
                    ServerLastUpdated = server.ServerLastUpdated.AddSeconds(1)
                };
            }
            DataSyncService sync;
            if (existingContext)
            {
                ReorderAndRenameRemotely();
                sync = CreateSyncService();
            }
            else
            {
                _data.AddContext(new ContextModel { ContextName = "Errands" });
                sync = await SyncWithPendingContextPost(ReorderAndRenameRemotely);
                Assert.Equal(5, Assert.Single(_state.GetChangedContextData()).OrderIndex);
                Assert.Equal(0, _api.ContextUpdates);
                Assert.Equal("Other device", Assert.Single(_api.Contexts).ContextName);
                AssertSingleContextInsert();
            }

            Assert.True(await sync.TrySync());
            Assert.Equal("Other device", Assert.Single(_api.Contexts).ContextName);
            Assert.Equal(0, Assert.Single(_api.Contexts).OrderIndex);
            Assert.Equal(1, _api.ContextUpdates);
            AssertLocalContextsMatchServer();
            AssertContextSyncCounts(0, 1, 0);
        }

        [Fact]
        public async Task SecondReorderDuringConflictReplyPreservesRemoteRename()
        {
            AddExistingContext();
            await LoadAsync();
            var working = Assert.Single(_data.GetDataStateContexts()!);
            working.OrderIndex = 1;
            _data.UpdateContextData(working);

            var remote = Assert.Single(_api.Contexts);
            remote.ContextName = "Other device";
            remote.ClientLastUpdated = remote.ServerLastUpdated.AddSeconds(1);
            _api.BeforeContextPutReply = () =>
            {
                working.OrderIndex = 2;
                _data.UpdateContextData(working);
            };

            var sync = CreateSyncService();
            Assert.True(await sync.TrySync());
            Assert.Equal("Other device", Assert.Single(_api.Contexts).ContextName);
            Assert.True(await sync.TrySync());

            remote = Assert.Single(_api.Contexts);
            Assert.Equal("Other device", remote.ContextName);
            Assert.Equal(0, remote.OrderIndex);
            Assert.Equal(1, _api.ContextUpdates);
            AssertLocalContextsMatchServer();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RemoteDeleteDuringUpdateReplyAllowsNextSync(bool duringAnotherPut)
        {
            AddExistingContext();
            if (duringAnotherPut) AddExistingContext(71, "Office", 1);
            await LoadAsync();
            RenameContext("Errands", "Notes");

            void DeleteFromBothClients()
            {
                _data.DeleteContext(Assert.Single(_data.GetDataStateContexts()!, context => context.Id == 70));
                _api.Contexts.RemoveAll(context => context.Id == 70);
            }
            _api.BeforeContextPutReply = DeleteFromBothClients;
            if (duringAnotherPut)
            {
                RenameContext("Office", "Work");
                _api.BeforeContextPutReply = () => _api.BeforeContextPutReply = DeleteFromBothClients;
            }

            var sync = CreateSyncService();
            Assert.True(await sync.TrySync());
            Assert.DoesNotContain(_state.GetContexts()!, context => context.Id == 70);
            Assert.DoesNotContain(_data.GetDataStateContexts()!, context => context.Id == 70);
            Assert.True(await sync.TrySync());

            Assert.DoesNotContain(_api.Contexts, context => context.Id == 70);
            AssertLocalContextsMatchServer();
        }

        [Fact]
        public async Task EditDuringUpdateLookupIsSentInOnePut()
        {
            AddExistingContext();
            await LoadAsync();
            RenameContext("Errands", "First edit");
            _api.BeforeContextGetReply = () => RenameContext("First edit", "Final edit");

            var sync = CreateSyncService();
            Assert.True(await sync.TrySync());
            Assert.Equal("Final edit", Assert.Single(_api.Contexts).ContextName);
            Assert.Equal(1, _api.ContextUpdates);
            Assert.True(await sync.TrySync());

            Assert.Equal(1, _api.ContextUpdates);
            AssertLocalContextsMatchServer();
        }

        [Fact]
        public async Task DeletionDuringUpdateSurvivesALaterFailedReply()
        {
            AddExistingContext();
            AddExistingContext(71, "Office", 1);
            await LoadAsync();
            RenameContext("Errands", "Notes");
            RenameContext("Office", "Work");
            _api.BeforeContextPutReply = () =>
            {
                _data.DeleteContext(Assert.Single(_data.GetDataStateContexts()!, context => context.Id == 70));
                _api.BeforeContextPutReply = () => throw new TaskCanceledException("The reply was lost after the update.");
            };

            var sync = CreateSyncService();
            Assert.False(await sync.TrySync());

            _data.ResetDataStateOnLogout();
            Assert.True(await sync.TrySync()); // Signing in reloads the server lists.
            Assert.True(await sync.TrySync());

            Assert.DoesNotContain(_api.Contexts, context => context.Id == 70);
            Assert.Equal("Work", Assert.Single(_api.Contexts).ContextName);
            Assert.Equal("Work", Assert.Single(_state.GetContexts()!).ContextName);
            Assert.Equal("Work", Assert.Single(_data.GetDataStateContexts()!).ContextName);
            Assert.Empty(_state.GetChangedContextData());
            Assert.Equal(ESyncStatus.Synchronized, _data.GetCurrentSyncStatus());
        }

        [Fact]
        public async Task DeletionDuringALaterUpdateKeepsTheContextHidden()
        {
            AddExistingContext();
            AddExistingContext(71, "Office", 1);
            await LoadAsync();
            RenameContext("Errands", "Notes");
            RenameContext("Office", "Work");
            _api.BeforeContextPutReply = () => _api.BeforeContextPutReply = () =>
                _data.DeleteContext(Assert.Single(_data.GetDataStateContexts()!, context => context.Id == 70));

            var sync = CreateSyncService();
            Assert.True(await sync.TrySync());
            Assert.DoesNotContain(_state.GetContexts()!, context => context.Id == 70);
            Assert.DoesNotContain(_data.GetDataStateContexts()!, context => context.Id == 70);

            Assert.True(await sync.TrySync());
            Assert.DoesNotContain(_state.GetContexts()!, context => context.Id == 70);
            Assert.DoesNotContain(_data.GetDataStateContexts()!, context => context.Id == 70);
        }

        [Fact]
        public async Task EditOfAnUnqueuedContextDuringPushIsSentOnNextSync()
        {
            AddExistingContext();
            AddExistingContext(71, "Office", 1);
            await LoadAsync();
            RenameContext("Errands", "Notes");
            _api.BeforeContextPutReply = () => RenameContext("Office", "Work");

            var sync = CreateSyncService();
            Assert.True(await sync.TrySync());
            Assert.Equal("Work", Assert.Single(_state.GetChangedContextData()).ContextName);

            Assert.True(await sync.TrySync());
            Assert.Equal(["Notes", "Work"], _api.Contexts.Select(context => context.ContextName));
            AssertLocalContextsMatchServer();
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, true)]
        [InlineData(true, false)]
        public async Task DeletingContextDuringPullKeepsItHidden(bool existingContext, bool updateBeforePull)
        {
            if (existingContext) AddExistingContext();
            await LoadAsync();
            if (!existingContext) _data.AddContext(new ContextModel { ContextName = "Errands" });
            if (updateBeforePull) RenameContext("Errands", "Home");
            _api.BeforePull = () => _data.DeleteContext(Assert.Single(_data.GetDataStateContexts()!));

            var sync = CreateSyncService();
            Assert.True(await sync.TrySync());
            Assert.Empty(_state.GetContexts()!);
            Assert.Empty(_data.GetDataStateContexts()!);
            var pendingDelete = Assert.Single(_state.GetChangedContextData());
            Assert.Equal(Assert.Single(_api.Contexts).Id, pendingDelete.Id);
            Assert.NotNull(pendingDelete.Deleted);
            AssertContextSyncCounts(existingContext ? 0 : 1, updateBeforePull ? 1 : 0, 0);

            Assert.True(await sync.TrySync());
            Assert.Empty(_api.Contexts);
            AssertLocalContextsMatchServer();
            AssertContextSyncCounts(0, 0, 1);
        }

        [Theory]
        [InlineData(false, 1)]
        [InlineData(true, 2)]
        public async Task RetryingCreateAfterSigningBackInLoadsAllServerContexts(bool committedBeforeFailure, int expectedRows)
        {
            await LoadAsync();
            _data.AddContext(new ContextModel { ContextName = "Errands" });
            _api.FailNextCreate = true;
            _api.CommitBeforeFailure = committedBeforeFailure;
            var sync = CreateSyncService();

            Assert.False(await sync.TrySync());
            Assert.Single(_state.GetChangedContextData());
            Assert.Equal(committedBeforeFailure ? 1 : 0, _api.Contexts.Count);

            _data.ResetDataStateOnLogout();
            Assert.True(await sync.TrySync()); // Signing in reloads the server lists.
            Assert.True(await sync.TrySync()); // Retry the pending create.

            Assert.Equal(expectedRows, _api.Contexts.Count);
            AssertLocalContextsMatchServer();
            AssertSingleContextInsert();
        }

        public void Dispose() => _api.Dispose();

        private async Task LoadAsync()
        {
            await _data.FetchAllRemoteData();
            _state.SetLastSync(DateTimeOffset.UnixEpoch);
        }

        private DataSyncService CreateSyncService() =>
            new(_mapper, _helper, _state, _users, _tasks, _projects, _contexts, NullLogger<DataSyncService>.Instance, _data);

        private async Task<DataSyncService> SyncWithPendingContextPost(Action change, string? heldPostName = null)
        {
            _api.ReleasePost = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _api.HeldPostName = heldPostName;
            var sync = CreateSyncService();
            var pending = sync.TrySync();
            try
            {
                await _api.PostStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
                change();
            }
            finally
            {
                _api.ReleasePost.SetResult();
            }
            Assert.True(await pending);
            return sync;
        }

        private void RenameContext(string oldName, string newName)
        {
            var working = _data.GetDataStateContexts()!.Single(context => context.ContextName == oldName);
            working.ContextName = newName;
            _data.UpdateContextData(working);
        }

        private void AddExistingContext(int id = 70, string name = "Errands", int orderIndex = 0) => _api.Contexts.Add(new ContextModel
        {
            Id = id, UserId = "test-user", ContextName = name, OrderIndex = orderIndex,
            ClientLastUpdated = DateTimeOffset.UnixEpoch, ServerLastUpdated = DateTimeOffset.Now
        });

        private static void AssertAcknowledged(ContextModel inserted, ContextDisplayModel? context)
        {
            Assert.NotNull(context);
            Assert.Equal(inserted.ContextName, context.ContextName);
            Assert.Equal(inserted.Id, context.Id);
            Assert.Equal(inserted.UserId, context.UserId);
            Assert.Equal(inserted.ServerLastUpdated, context.ServerLastUpdated);
            Assert.Null(context.TempLocalId);
        }

        private void AssertLocalContextsMatchServer()
        {
            foreach (var local in new[] { _state.GetContexts()!, _data.GetDataStateContexts()! })
            {
                Assert.Equal(_api.Contexts.Count, local.Count);
                foreach (var server in _api.Contexts)
                {
                    var context = Assert.Single(local, x => x.Id == server.Id);
                    AssertAcknowledged(server, context);
                    Assert.Equal(server.OrderIndex, context.OrderIndex);
                }
            }
            Assert.Empty(_state.GetChangedContextData());
            Assert.Equal(ESyncStatus.Synchronized, _data.GetCurrentSyncStatus());
        }

        private void AssertSingleContextInsert() => AssertContextSyncCounts(1, 0, 0);

        private void AssertContextSyncCounts(int inserted, int updated, int deleted)
        {
            var result = _data.GetDataStateLastSyncResult();
            Assert.Equal((inserted, updated, deleted), (result.NumRowsInserted, result.NumRowsUpdated, result.NumRowsDeleted));
        }

        private sealed class ContextApi : HttpMessageHandler, IAPIHelper
        {
            private const string UserId = "test-user";

            public ContextApi() => APIClient = new HttpClient(this, disposeHandler: false) { BaseAddress = new Uri("https://taskfocus.test") };

            public HttpClient APIClient { get; }
            public List<ContextModel> Contexts { get; } = [];
            public List<ProjectModel> Projects { get; } = [];
            public bool ReturnContextForLegacyPost { get; set; }
            public Action? BeforePull { get; set; }
            public Action? BeforeContextGetReply { get; set; }
            public Action? BeforeContextPutReply { get; set; }
            public TaskCompletionSource PostStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource? ReleasePost { get; set; }
            public string? HeldPostName { get; set; }
            public int ContextUpdates { get; private set; }
            public bool FailNextCreate { get; set; }
            public bool CommitBeforeFailure { get; set; }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                string path = request.RequestUri!.AbsolutePath;
                switch (request.Method.Method, path)
                {
                    case ("POST", "/api/context/post"):
                    case ("POST", "/api/context/PostWithResult"):
                        bool fail = FailNextCreate;
                        FailNextCreate = false;
                        if (fail && !CommitBeforeFailure)
                            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);

                        var inserted = await request.Content!.ReadFromJsonAsync<ContextModel>(cancellationToken);
                        Assert.NotNull(inserted);
                        inserted.Id = 100 + Contexts.Count;
                        inserted.ServerLastUpdated = inserted.ServerLastUpdated.AddTicks(1);
                        Contexts.Add(inserted);
                        if (fail) throw new TaskCanceledException("The response timed out after the insert.");
                        if (ReleasePost != null && (HeldPostName == null || HeldPostName == inserted.ContextName) && PostStarted.TrySetResult())
                            await ReleasePost.Task.WaitAsync(cancellationToken);
                        return path == "/api/context/PostWithResult" || ReturnContextForLegacyPost
                            ? JsonResponse(inserted)
                            : new HttpResponseMessage(HttpStatusCode.OK);

                    case ("PUT", "/api/context/put"):
                        var updated = await request.Content!.ReadFromJsonAsync<ContextModel>(cancellationToken);
                        Assert.NotNull(updated);
                        Contexts[Contexts.FindIndex(x => x.Id == updated.Id)] = updated;
                        ContextUpdates++;
                        var beforePutReply = BeforeContextPutReply;
                        BeforeContextPutReply = null;
                        beforePutReply?.Invoke();
                        return new HttpResponseMessage(HttpStatusCode.OK);

                    case ("DELETE", "/api/context/delete"):
                        var deleted = await request.Content!.ReadFromJsonAsync<ContextModel>(cancellationToken);
                        Assert.NotNull(deleted);
                        Contexts.Remove(Contexts.Single(context => context.Id == deleted.Id));
                        return new HttpResponseMessage(HttpStatusCode.OK);

                    case ("GET", "/api/user/GetCurrentUser"):
                        var beforePull = BeforePull;
                        BeforePull = null;
                        beforePull?.Invoke();
                        return JsonResponse(new UserModel { Id = UserId });

                    case ("GET", "/api/user/GetCurrentUserSettings"):
                        return JsonResponse(new UserSettingsModel());

                    case ("GET", "/api/task/GetAllTasksForUser"):
                        return JsonResponse(Array.Empty<TaskModel>());

                    case ("GET", "/api/project/GetAllProjectsForUser"):
                        return JsonResponse(Projects);

                    case ("GET", "/api/context/GetAllContextsForUser"):
                        return JsonResponse(Contexts);

                    case ("GET", _) when path.StartsWith("/api/context/GetContextById/", StringComparison.Ordinal):
                        int id = int.Parse(path.Split('/')[^1]);
                        var row = Contexts.SingleOrDefault(context => context.Id == id);
                        var beforeGetReply = BeforeContextGetReply;
                        BeforeContextGetReply = null;
                        beforeGetReply?.Invoke();
                        return row == null ? new HttpResponseMessage(HttpStatusCode.NoContent) : JsonResponse(row);

                    default:
                        throw new InvalidOperationException($"Unexpected request {request.Method} {path}");
                }
            }

            private static HttpResponseMessage JsonResponse<T>(T value) =>
                new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };

            protected override void Dispose(bool disposing)
            {
                if (disposing) APIClient.Dispose();
                base.Dispose(disposing);
            }

            public string GetLoggedInUserId() => UserId;
            public Task<AuthenticatedUser> AuthenticateAsync(string username, string password) => throw new NotSupportedException();
            public Task GetLoggedInUserInfoAsync(string token) => throw new NotSupportedException();
            public void LogOutUser() => throw new NotSupportedException();
        }
    }
}
