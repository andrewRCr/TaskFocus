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
    public sealed class EditThenDeleteTests : IDisposable
    {
        private readonly ItemApi _api = new();
        private readonly DataState _state = new();
        private readonly DataService _data;
        private readonly DataSyncService _sync;

        public EditThenDeleteTests()
        {
            IMapper mapper = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<UserModel, UserDisplayModel>();
                cfg.CreateMap<UserSettingsModel, UserSettingsDisplayModel>();
                cfg.CreateMap<TaskModel, TaskDisplayModel>().ReverseMap();
                cfg.CreateMap<ProjectModel, ProjectDisplayModel>().ReverseMap();
                cfg.CreateMap<ContextModel, ContextDisplayModel>().ReverseMap();
            }).CreateMapper();

            var helper = new DataHelper(mapper, _state);
            var users = new UserEndpoint(_api);
            var tasks = new TaskEndpoint(_api);
            var projects = new ProjectEndpoint(_api);
            var contexts = new ContextEndpoint(_api);
            _data = new DataService(mapper, helper, _state, users, tasks, projects, contexts, NullLogger<DataService>.Instance);
            _sync = new DataSyncService(mapper, helper, _state, users, tasks, projects, contexts, NullLogger<DataSyncService>.Instance, _data);
        }

        [Theory]
        [InlineData("task")]
        [InlineData("project")]
        [InlineData("context")]
        public async Task DeletingAnEditedItemDeletesItOnTheServer(string kind)
        {
            await LoadAsync();
            Rename(kind, 1);
            Delete(kind, 1);

            await SyncOnceAsync();

            Assert.Equal([$"DELETE {kind} 1"], _api.Writes);
            Assert.Equal([2], LocalIds(kind));
            AssertCounts(updated: 0, deleted: 1);
        }

        [Fact]
        public async Task DeletingAnEditedTaskStillSendsTheOtherEdits()
        {
            await LoadAsync();
            Rename("task", 1);
            Rename("task", 2);
            Delete("task", 1);

            await SyncOnceAsync();

            Assert.Equal(["DELETE task 1", "PUT task 2"], _api.Writes);
            Assert.Equal("Renamed", Assert.Single(_api.Tasks).TaskName);
            Assert.Equal([2], LocalIds("task"));
            AssertCounts(updated: 1, deleted: 1);
        }

        [Fact]
        public async Task DeletingAnEditedTaskWhileAnotherEditIsSendingDeletesIt()
        {
            await LoadAsync();
            Rename("task", 1);
            Rename("task", 2);
            _api.BeforePutReply = () => Delete("task", 2);

            await SyncOnceAsync();

            Assert.Equal(["PUT task 1", "DELETE task 2"], _api.Writes);
            Assert.Equal([1], LocalIds("task"));
            AssertCounts(updated: 1, deleted: 1);
        }

        public void Dispose() => _api.Dispose();

        private async Task LoadAsync()
        {
            await _data.FetchAllRemoteData();
            _state.SetLastSync(DateTimeOffset.UnixEpoch);
            _api.Pulls = 0;
        }

        private async Task SyncOnceAsync()
        {
            Assert.True(await _sync.TrySync());
            Assert.Equal(1, _api.Pulls); // No failed attempt was retried.
            Assert.Equal(ESyncStatus.Synchronized, _data.GetCurrentSyncStatus());
            Assert.Empty(_state.GetChangedTaskData());
            Assert.Empty(_state.GetChangedProjectData());
            Assert.Empty(_state.GetChangedContextData());
        }

        private void Rename(string kind, int id)
        {
            switch (kind)
            {
                case "task":
                    var task = _data.GetDataStateTasks()!.Single(x => x.Id == id);
                    task.TaskName = "Renamed";
                    _data.UpdateTaskData(task);
                    Assert.Contains(_state.GetChangedTaskData(), x => x.Id == id);
                    break;

                case "project":
                    var project = _data.GetDataStateProjects()!.Single(x => x.Id == id);
                    project.ProjectName = "Renamed";
                    _data.UpdateProjectData(project);
                    Assert.Contains(_state.GetChangedProjectData(), x => x.Id == id);
                    break;

                case "context":
                    var context = _data.GetDataStateContexts()!.Single(x => x.Id == id);
                    context.ContextName = "Renamed";
                    _data.UpdateContextData(context);
                    Assert.Contains(_state.GetChangedContextData(), x => x.Id == id);
                    break;
            }
        }

        private void Delete(string kind, int id)
        {
            switch (kind)
            {
                case "task": _data.DeleteTask(_data.GetDataStateTasks()!.Single(x => x.Id == id)); break;
                case "project": _data.DeleteProject(_data.GetDataStateProjects()!.Single(x => x.Id == id)); break;
                case "context": _data.DeleteContext(_data.GetDataStateContexts()!.Single(x => x.Id == id)); break;
            }
        }

        private IEnumerable<int> LocalIds(string kind) => kind switch
        {
            "task" => _data.GetDataStateTasks()!.Select(x => x.Id!.Value),
            "project" => _data.GetDataStateProjects()!.Select(x => x.Id!.Value),
            _ => _data.GetDataStateContexts()!.Select(x => x.Id!.Value)
        };

        private void AssertCounts(int updated, int deleted)
        {
            var result = _data.GetDataStateLastSyncResult();
            Assert.Equal((updated, deleted), (result.NumRowsUpdated, result.NumRowsDeleted));
        }

        // Two rows of each kind, last changed before the sync under test.
        private sealed class ItemApi : HttpMessageHandler, IAPIHelper
        {
            private const string UserId = "test-user";

            public ItemApi()
            {
                APIClient = new HttpClient(this, disposeHandler: false) { BaseAddress = new Uri("https://taskfocus.test") };
                for (int id = 1; id <= 2; id++)
                {
                    Tasks.Add(new TaskModel { Id = id, UserId = UserId, TaskName = $"Task {id}", InboxIndex = id - 1 });
                    Projects.Add(new ProjectModel { Id = id, UserId = UserId, ProjectName = $"Project {id}", OrderIndex = id - 1 });
                    Contexts.Add(new ContextModel { Id = id, UserId = UserId, ContextName = $"Context {id}", OrderIndex = id - 1 });
                }
            }

            public HttpClient APIClient { get; }
            public List<TaskModel> Tasks { get; } = [];
            public List<ProjectModel> Projects { get; } = [];
            public List<ContextModel> Contexts { get; } = [];
            public List<string> Writes { get; } = [];
            public int Pulls { get; set; }
            public Action? BeforePutReply { get; set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                string[] path = request.RequestUri!.AbsolutePath.Split('/'); // "", "api", kind, action, [id]
                switch (path[2])
                {
                    case "task": return HandleAsync("task", Tasks, x => x.Id, path, request, cancellationToken);
                    case "project": return HandleAsync("project", Projects, x => x.Id, path, request, cancellationToken);
                    case "context": return HandleAsync("context", Contexts, x => x.Id, path, request, cancellationToken);
                    case "user" when path[3] == "GetCurrentUser":
                        Pulls++;
                        return Task.FromResult(JsonResponse(new UserModel { Id = UserId }));
                    case "user" when path[3] == "GetCurrentUserSettings":
                        return Task.FromResult(JsonResponse(new UserSettingsModel()));
                    default: throw new InvalidOperationException($"Unexpected request {request.Method} {request.RequestUri}");
                }
            }

            private async Task<HttpResponseMessage> HandleAsync<T>(
                string kind, List<T> rows, Func<T, int?> id, string[] path, HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (request.Method == HttpMethod.Get)
                {
                    return path.Length == 5
                        ? JsonResponse(rows.Single(x => id(x) == int.Parse(path[4])))
                        : JsonResponse(rows);
                }

                var sent = (await request.Content!.ReadFromJsonAsync<T>(cancellationToken))!;
                int index = rows.FindIndex(x => id(x) == id(sent));
                Writes.Add($"{request.Method} {kind} {id(sent)}");
                if (request.Method == HttpMethod.Put) rows[index] = sent;
                else if (request.Method == HttpMethod.Delete) rows.RemoveAt(index);
                else throw new InvalidOperationException($"Unexpected request {request.Method} {request.RequestUri}");

                if (request.Method == HttpMethod.Put)
                {
                    var beforeReply = BeforePutReply;
                    BeforePutReply = null;
                    beforeReply?.Invoke();
                }
                return new HttpResponseMessage(HttpStatusCode.OK);
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
