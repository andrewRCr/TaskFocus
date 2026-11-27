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
    public sealed class TaskCompletionTests : IDisposable
    {
        private readonly TaskApi _api = new();
        private readonly DataState _state = new();
        private readonly DataHelper _helper;
        private readonly DataService _data;
        private readonly DataSyncService _sync;

        public TaskCompletionTests()
        {
            IMapper mapper = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<UserModel, UserDisplayModel>();
                cfg.CreateMap<UserSettingsModel, UserSettingsDisplayModel>();
                cfg.CreateMap<TaskModel, TaskDisplayModel>().ReverseMap();
                cfg.CreateMap<ProjectModel, ProjectDisplayModel>().ReverseMap();
                cfg.CreateMap<ContextModel, ContextDisplayModel>().ReverseMap();
            }).CreateMapper();

            _helper = new DataHelper(mapper, _state);
            var users = new UserEndpoint(_api);
            var tasks = new TaskEndpoint(_api);
            var projects = new ProjectEndpoint(_api);
            var contexts = new ContextEndpoint(_api);
            _data = new DataService(mapper, _helper, _state, users, tasks, projects, contexts, NullLogger<DataService>.Instance);
            _sync = new DataSyncService(mapper, _helper, _state, users, tasks, projects, contexts, NullLogger<DataSyncService>.Instance, _data);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CheckingOffATaskSavesItAndOtherEditsAndKeepsSyncRunning(bool cleanUpImmediately)
        {
            _api.Settings.CleanUpImmediately = cleanUpImmediately;
            _api.Add(1);
            _api.Add(2);
            await SignInAsync();

            SetCompleted(1, true);
            Assert.True(IsRecent(Working(1).DateCompleted));
            Rename(2, "Renamed");
            await SyncAsync();

            Assert.Equal(["PUT 1", "PUT 2"], _api.Writes.Order());
            Assert.Equal((true, cleanUpImmediately), (_api.Task(1).Completed, _api.Task(1).CleanedUp));
            Assert.NotNull(_api.Task(1).DateCompleted);
            Assert.Equal("Renamed", _api.Task(2).TaskName);

            Rename(2, "Renamed again");
            await SyncAsync();

            Assert.Equal("Renamed again", _api.Task(2).TaskName);
            Assert.Equal(["PUT 1", "PUT 2", "PUT 2"], _api.Writes.Order());
        }

        [Fact]
        public async Task ANewTaskCheckedOffBeforeItIsSentIsSavedAsCompleted()
        {
            _api.Add(1);
            await SignInAsync();

            _data.AddTask(new TaskDisplayModel { TaskName = "New" });
            var added = _data.GetDataStateTasks()!.Single(x => x.TaskName == "New");
            added.Completed = true;
            _data.UpdateTaskData(added);
            Assert.True(IsRecent(added.DateCompleted));
            await SyncAsync();

            Assert.Equal(["POST New"], _api.Writes);
            Assert.True(_api.Tasks.Single(x => x.TaskName == "New").Completed);
        }

        [Theory]
        [InlineData(5)]
        [InlineData(null)]
        public async Task EditingACompletedTaskKeepsItsCompletionDate(int? daysAgo)
        {
            DateTime? completed = daysAgo is int d ? DateTime.Now.Date.AddDays(-d) : null;
            _api.Settings.CleanUpImmediately = true;
            _api.Add(1, completed: true, dateCompleted: completed, starred: true, todayIndex: 0);
            await SignInAsync();

            var task = Working(1);
            task.Starred = false;
            _data.UpdateTaskData(task);
            foreach (var copy in Copies(1)) Assert.Equal(completed, copy.DateCompleted);
            await SyncAsync();

            Assert.Equal(completed, _api.Task(1).DateCompleted);
            Assert.Equal(daysAgo != null, _api.Task(1).CleanedUp);
        }

        [Fact]
        public async Task ACompletedTaskWithoutADateDoesNotStopSignInOrSync()
        {
            _api.Add(1, completed: true);
            _api.Add(2);
            await SignInAsync();

            Rename(2, "Renamed");
            await SyncAsync();

            Assert.Equal(["PUT 2"], _api.Writes);
        }

        [Fact]
        public async Task ReopeningATaskForgetsWhenItWasCompleted()
        {
            _api.Add(1, completed: true, dateCompleted: DateTime.Now.AddDays(-5));
            await SignInAsync();

            SetCompleted(1, false);
            foreach (var copy in Copies(1)) Assert.Null(copy.DateCompleted);
            await SyncAsync();
            Assert.Equal((false, null), (_api.Task(1).Completed, _api.Task(1).DateCompleted));

            SetCompleted(1, true);
            foreach (var copy in Copies(1)) Assert.True(IsRecent(copy.DateCompleted));
            await SyncAsync();
            Assert.True(_api.Task(1).Completed && IsRecent(_api.Task(1).DateCompleted));
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public async Task CheckingOffATaskTakesItOutOfTodayAndReopeningPutsItLast(bool starred, bool due)
        {
            var today = DateTime.Now.Date;
            for (int id = 1; id <= 3; id++) _api.Add(id, starred: starred, dueDate: due ? today : null, todayIndex: id - 1);
            await SignInAsync();

            SetCompleted(1, true);
            var checkedOff = TodayPlaces();
            await SyncAsync();
            var synced = TodayPlaces();
            SetCompleted(1, false);
            var reopened = TodayPlaces();
            await SyncAsync();
            _data.ResetDataStateOnLogout();
            await SignInAsync();
            var signedInAgain = TodayPlaces();

            if (DateTime.Now.Date != today) return; // Midnight passed during the test.
            Assert.Equal([(2, 1), (3, 2)], checkedOff);
            Assert.Equal([(2, 1), (3, 2)], synced);
            Assert.Equal([(2, 1), (3, 2), (1, 3)], reopened);
            Assert.Equal([(2, 1), (3, 2), (1, 3)], signedInAgain);
        }

        [Fact]
        public async Task ANewDueTaskCheckedOffBeforeItIsSentIsNotInToday()
        {
            var today = DateTime.Now.Date;
            _api.Add(1, dueDate: today, todayIndex: 0);
            await SignInAsync();

            _data.AddTask(new TaskDisplayModel { TaskName = "New", DueDate = today });
            var added = _data.GetDataStateTasks()!.Single(x => x.TaskName == "New");
            added.Completed = true;
            _data.UpdateTaskData(added);
            await SyncAsync();

            if (DateTime.Now.Date != today) return; // Midnight passed during the test.
            Assert.Equal([(1, 0)], TodayPlaces());
            Assert.Null(_api.Tasks.Single(x => x.TaskName == "New").TodayIndex);
        }

        public void Dispose() => _api.Dispose();

        private async Task SignInAsync() => Assert.True(await _sync.TrySync(onFailure: Rethrow));

        private async Task SyncAsync()
        {
            Assert.True(await _sync.TrySync(onFailure: Rethrow));
            Assert.Equal(ESyncStatus.Synchronized, _data.GetCurrentSyncStatus());
            Assert.Empty(_state.GetChangedTaskData());
        }

        private static void Rethrow(Exception exception) => Assert.Fail($"The sync failed: {exception.Message}");

        private static bool IsRecent(DateTime? date) => date is DateTime d && Math.Abs((DateTime.UtcNow - d).TotalMinutes) < 5;

        private TaskDisplayModel Working(int id) => _data.GetDataStateTasks()!.Single(x => x.Id == id);

        private IEnumerable<TaskDisplayModel> Copies(int id) =>
            [Working(id), _state.GetTasks()!.Single(x => x.Id == id), _state.GetChangedTaskData().Single(x => x.Id == id)];

        private List<(int, int?)> TodayPlaces() => [.. _helper.GetTodayWorkingTasks().OrderBy(x => x.TodayIndex).Select(x => (x.Id ?? 0, x.TodayIndex))];

        private void SetCompleted(int id, bool completed)
        {
            Working(id).Completed = completed;
            _data.UpdateTaskData(Working(id));
        }

        private void Rename(int id, string name)
        {
            Working(id).TaskName = name;
            _data.UpdateTaskData(Working(id));
        }

        // Sets and clears DateCompleted, in UTC, as the server does.
        private sealed class TaskApi : HttpMessageHandler, IAPIHelper
        {
            private const string UserId = "test-user";

            public TaskApi() => APIClient = new HttpClient(this, disposeHandler: false) { BaseAddress = new Uri("https://taskfocus.test") };

            public HttpClient APIClient { get; }
            public List<TaskModel> Tasks { get; } = [];
            public List<string> Writes { get; } = [];
            public UserSettingsModel Settings { get; } = new() { CleanUpDelayDays = 7, DeleteDelayDays = 30 };

            public TaskModel Task(int id) => Tasks.Single(x => x.Id == id);

            public void Add(int id, bool completed = false, DateTime? dateCompleted = null, bool starred = false,
                DateTime? dueDate = null, int? todayIndex = null) => Tasks.Add(new TaskModel
            {
                Id = id, UserId = UserId, TaskName = $"Task {id}", InboxIndex = Tasks.Count, Completed = completed,
                DateCompleted = dateCompleted, Starred = starred, DueDate = dueDate, TodayIndex = todayIndex
            });

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                string path = request.RequestUri!.AbsolutePath;
                switch (request.Method.Method, path)
                {
                    case ("GET", "/api/user/GetCurrentUser"): return JsonResponse(new UserModel { Id = UserId });
                    case ("GET", "/api/user/GetCurrentUserSettings"): return JsonResponse(Settings);
                    case ("GET", "/api/task/GetAllTasksForUser"): return JsonResponse(Tasks);
                    case ("GET", "/api/project/GetAllProjectsForUser"): return JsonResponse(Array.Empty<ProjectModel>());
                    case ("GET", "/api/context/GetAllContextsForUser"): return JsonResponse(Array.Empty<ContextModel>());

                    case ("GET", _) when path.StartsWith("/api/task/GetTaskById/", StringComparison.Ordinal):
                        return JsonResponse(Task(int.Parse(path.Split('/')[^1])));

                    case ("POST", "/api/task/post"):
                        var added = (await request.Content!.ReadFromJsonAsync<TaskModel>(cancellationToken))!;
                        added.Id = 100 + Tasks.Count;
                        added.DateCompleted = added.Completed ? DateTime.UtcNow : null;
                        Tasks.Add(added);
                        Writes.Add($"POST {added.TaskName}");
                        return JsonResponse(added);

                    case ("PUT", "/api/task/put"):
                        var sent = (await request.Content!.ReadFromJsonAsync<TaskModel>(cancellationToken))!;
                        var stored = Task(sent.Id!.Value);
                        sent.DateCompleted = !sent.Completed ? null : stored.Completed ? stored.DateCompleted : DateTime.UtcNow;
                        if (stored.Completed && !sent.Completed) sent.CleanedUp = false;
                        Tasks[Tasks.IndexOf(stored)] = sent;
                        Writes.Add($"PUT {sent.Id}");
                        return new HttpResponseMessage(HttpStatusCode.OK);

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
