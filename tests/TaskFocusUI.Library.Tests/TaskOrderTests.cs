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
    // Positions only order a list, so removing a task must leave the others' positions, and the server's, as they were.
    public sealed class TaskOrderTests : IDisposable
    {
        private readonly OrderApi _api = new();
        private readonly DataState _state = new();
        private readonly DataHelper _helper;
        private readonly DataService _data;
        private readonly DataSyncService _sync;

        public TaskOrderTests()
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
        [InlineData("Inbox")]
        [InlineData("Project")]
        [InlineData("Context")]
        [InlineData("Today")]
        public async Task DeletingATaskLeavesTheOthersWhereTheyAre(string list)
        {
            SeedFour(list);
            await SignInAsync();

            _data.DeleteTask(Working(1));
            AssertOrder(list, [2, 3, 4], [1, 2, 3]);

            await SyncAsync();
            AssertOrder(list, [2, 3, 4], [1, 2, 3]);
            Assert.Equal(["DELETE 1"], _api.Writes);

            await SignInAgainAsync();
            AssertOrder(list, [2, 3, 4], [1, 2, 3]);
        }

        [Theory]
        [InlineData("Inbox")]
        [InlineData("Project")]
        [InlineData("Context")]
        [InlineData("Today")]
        public async Task ATaskAddedAfterOthersAreDeletedGoesLast(string list)
        {
            SeedFour(list);
            await SignInAsync();
            _data.DeleteTask(Working(1));
            _data.DeleteTask(Working(2));

            _data.AddTask(new TaskDisplayModel
            {
                TaskName = "New",
                ProjectName = list == "Project" ? "Project 7" : null,
                ContextName = list == "Context" ? "Context 9" : null,
                Starred = list == "Today"
            });
            Assert.NotNull(Position(Ordered(list)[^1], list));
            Assert.Equal("New", Ordered(list)[^1].TaskName);

            await SyncAsync();
            Assert.Equal(["3", "4", "New"], Names(list));
            await SignInAgainAsync();
            Assert.Equal(["3", "4", "New"], Names(list));
        }

        [Fact]
        public async Task ANewTaskDueTodayGoesLastInTodayAtOnce()
        {
            var today = DateTime.Now.Date;
            _api.Add(1, starred: true, todayIndex: 0);
            _api.Add(2, starred: true, todayIndex: 4);
            await SignInAsync();

            _data.AddTask(new TaskDisplayModel { TaskName = "New", DueDate = today });
            var order = Names("Today");
            int? position = Ordered("Today").Single(x => x.TaskName == "New").TodayIndex;
            await SyncAsync();

            if (DateTime.Now.Date != today) return; // Midnight passed during the test.
            Assert.Equal(["1", "2", "New"], order);
            Assert.Equal((5, 5), (position, _api.Task(100).TodayIndex));
        }

        [Fact]
        public async Task ATaskDeletedOnAnotherDeviceLeavesTheOthersWhereTheyAre()
        {
            SeedFour("Inbox");
            await SignInAsync();
            _api.Tasks.RemoveAll(x => x.Id == 1);

            await SyncAsync();

            AssertOrder("Inbox", [2, 3, 4], [1, 2, 3]);
            Assert.Empty(_api.Writes);
        }

        [Fact]
        public async Task MovingATaskToAnotherProjectPutsItLastAndLeavesTheRest()
        {
            SeedFour("Project");
            _api.Add(5, projectId: 8, projectIndex: 4); // Project 8 has one task, after a gap.
            await SignInAsync();

            Working(1).ProjectName = "Project 8";
            _data.UpdateTaskData(Working(1));
            await SyncAsync();

            Assert.Equal(["PUT 1"], _api.Writes);
            Assert.Equal((8, 5), (_api.Task(1).ProjectId, _api.Task(1).ProjectIndex));
            AssertOrder("Project", [2, 3, 4], [1, 2, 3]);
        }

        [Fact]
        public async Task ATaskLeavingTheInboxLeavesTheRestWhereTheyAre()
        {
            SeedFour("Inbox");
            await SignInAsync();

            Working(1).ProjectName = "Project 7";
            _data.UpdateTaskData(Working(1));
            Working(1).ContextName = "Context 9";
            _data.UpdateTaskData(Working(1));
            await SyncAsync();

            Assert.Null(_api.Task(1).InboxIndex);
            AssertOrder("Inbox", [2, 3, 4], [1, 2, 3]);
            Assert.Equal(["PUT 1"], _api.Writes);
        }

        [Fact]
        public async Task StarringAndUnstarringATaskPutsItLastInTodayAndLeavesTheRest()
        {
            SeedFour("Today");
            _api.Add(5);
            await SignInAsync();

            SetStarred(1, false);
            SetStarred(5, true);
            await SyncAsync();

            Assert.Equal(["2", "3", "4", "5"], Names("Today"));
            Assert.Null(_api.Task(1).TodayIndex);
            Assert.Equal((1, 2, 3, 4), (_api.Task(2).TodayIndex, _api.Task(3).TodayIndex, _api.Task(4).TodayIndex, _api.Task(5).TodayIndex));
            Assert.Equal(["PUT 1", "PUT 5"], _api.Writes.Order());
        }

        [Theory]
        [InlineData(2, false)]
        [InlineData(2, true)]
        [InlineData(8, false)]
        [InlineData(8, true)]
        public async Task ACompletedTaskLeavingTodayLeavesTheRestWhereTheyAre(int daysAgo, bool bothStarredAndDue)
        {
            var today = DateTime.Now.Date;
            for (int id = 1; id <= 3; id++)
                _api.Add(id, inboxIndex: id - 1, starred: true, dueDate: bothStarredAndDue ? today : null, todayIndex: id - 1,
                    completed: id == 1, dateCompleted: id == 1 ? DateTime.UtcNow.AddDays(-daysAgo) : null);
            await SignInAsync();
            await SyncAsync();

            if (DateTime.Now.Date != today) return; // Midnight passed during the test.
            Assert.Equal(["PUT 1"], _api.Writes);
            Assert.Equal(daysAgo == 8, _api.Task(1).CleanedUp);
            Assert.Null(_api.Task(1).TodayIndex);
            Assert.Equal((1, 2), (_api.Task(2).TodayIndex, _api.Task(3).TodayIndex));
            Assert.Equal((1, 2), (_api.Task(2).InboxIndex, _api.Task(3).InboxIndex));
        }

        [Theory]
        [InlineData("Inbox")]
        [InlineData("Project")]
        [InlineData("Context")]
        [InlineData("Today")]
        public async Task ReopeningACleanedUpTaskPutsItBackLast(string list)
        {
            SeedFour(list);
            var cleaned = _api.Task(1);
            (cleaned.Completed, cleaned.CleanedUp, cleaned.DateCompleted) = (true, true, DateTime.Now.AddDays(-9));
            (cleaned.InboxIndex, cleaned.ProjectIndex, cleaned.ContextIndex, cleaned.TodayIndex) = (null, null, null, null);
            await SignInAsync();

            Working(1).Completed = false;
            _data.UpdateTaskData(Working(1));
            AssertOrder(list, [2, 3, 4, 1], [1, 2, 3, 4]);

            await SyncAsync();
            AssertOrder(list, [2, 3, 4, 1], [1, 2, 3, 4]);
            Assert.Equal(["PUT 1"], _api.Writes);
            Assert.False(_api.Task(1).CleanedUp);

            await SignInAgainAsync();
            AssertOrder(list, [2, 3, 4, 1], [1, 2, 3, 4]);
            Assert.DoesNotContain(_data.GetDataStateTasks()!, x => x.Completed);
        }

        [Fact]
        public async Task ANewlyDueTaskEntersTodayAfterTheHighestPosition()
        {
            var today = DateTime.Now.Date;
            _api.Add(1, starred: true, todayIndex: 3);
            _api.Add(2, dueDate: today);
            await SignInAsync();
            await SyncAsync();

            if (DateTime.Now.Date != today) return; // Midnight passed during the test.
            Assert.Equal((3, 4), (_api.Task(1).TodayIndex, _api.Task(2).TodayIndex));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task DeletingAProjectOrContextPutsItsTasksLastInTheInboxInOrder(bool project)
        {
            for (int id = 1; id <= 2; id++) _api.Add(id, inboxIndex: id * 2);
            for (int id = 3; id <= 4; id++) _api.Add(id, projectId: 7, projectIndex: id, contextId: 9, contextIndex: id);
            await SignInAsync();

            if (project) _data.DeleteProject(_data.GetDataStateProjects()!.Single(x => x.Id == 7));
            else _data.DeleteContext(_data.GetDataStateContexts()!.Single(x => x.Id == 9));
            await SyncAsync();
            await SignInAgainAsync();

            Assert.Equal(["1", "2", "3", "4"], Names("Inbox"));
            Assert.Equal((5, 6), (_api.Task(3).InboxIndex, _api.Task(4).InboxIndex));
        }

        public void Dispose() => _api.Dispose();

        private void SeedFour(string list)
        {
            for (int i = 0; i < 4; i++)
                _api.Add(i + 1,
                    inboxIndex: list == "Inbox" ? i : null,
                    projectId: list == "Project" ? 7 : null, projectIndex: list == "Project" ? i : null,
                    contextId: list == "Context" ? 9 : null, contextIndex: list == "Context" ? i : null,
                    starred: list == "Today", todayIndex: list == "Today" ? i : null);
        }

        private async Task SignInAsync()
        {
            Assert.True(await _sync.TrySync(onFailure: Rethrow));
        }

        private async Task SignInAgainAsync()
        {
            _data.ResetDataStateOnLogout();
            Assert.True(await _sync.TrySync(onFailure: Rethrow));
        }

        private async Task SyncAsync()
        {
            Assert.True(await _sync.TrySync(onFailure: Rethrow));
            Assert.Equal(ESyncStatus.Synchronized, _data.GetCurrentSyncStatus());
            Assert.Empty(_state.GetChangedTaskData());
        }

        private static void Rethrow(Exception exception) => Assert.Fail($"The sync failed: {exception.Message}");

        private TaskDisplayModel Working(int id) => _data.GetDataStateTasks()!.Single(x => x.Id == id);

        private void SetStarred(int id, bool starred)
        {
            Working(id).Starred = starred;
            _data.UpdateTaskData(Working(id));
        }

        private List<TaskDisplayModel> Ordered(string list) => list switch
        {
            "Inbox" => [.. _data.GetDataStateTasks()!.Where(x => (x.ProjectId == null || x.ContextId == null) && !x.CleanedUp).OrderBy(x => x.InboxIndex)],
            "Project" => _data.GetDataStateTasks()!.GetProjectTasks(7),
            "Context" => _data.GetDataStateTasks()!.GetContextTasks(9),
            _ => [.. _helper.GetTodayWorkingTasks().OrderBy(x => x.TodayIndex)]
        };

        private List<string> Names(string list) => [.. Ordered(list).Select(x => x.TaskName!)];

        private void AssertOrder(string list, int[] ids, int[] positions)
        {
            Assert.Equal(ids, Ordered(list).Select(x => x.Id!.Value));
            Assert.Equal(positions, Ordered(list).Select(x => Position(x, list)!.Value));
            foreach (int id in ids) Assert.Equal(positions[Array.IndexOf(ids, id)], Position(_state.GetTasks()!.Single(x => x.Id == id), list));
        }

        private static int? Position(TaskDisplayModel task, string list) => list switch
        {
            "Inbox" => task.InboxIndex, "Project" => task.ProjectIndex, "Context" => task.ContextIndex, _ => task.TodayIndex
        };

        private sealed class OrderApi : HttpMessageHandler, IAPIHelper
        {
            private const string UserId = "test-user";

            public OrderApi()
            {
                APIClient = new HttpClient(this, disposeHandler: false) { BaseAddress = new Uri("https://taskfocus.test") };
                Projects = [new ProjectModel { Id = 7, UserId = UserId, ProjectName = "Project 7", OrderIndex = 0 }, new ProjectModel { Id = 8, UserId = UserId, ProjectName = "Project 8", OrderIndex = 1 }];
                Contexts = [new ContextModel { Id = 9, UserId = UserId, ContextName = "Context 9", OrderIndex = 0 }];
            }

            public HttpClient APIClient { get; }
            public List<TaskModel> Tasks { get; } = [];
            public List<ProjectModel> Projects { get; }
            public List<ContextModel> Contexts { get; }
            public List<string> Writes { get; } = [];

            public TaskModel Task(int id) => Tasks.Single(x => x.Id == id);

            public void Add(int id, int? inboxIndex = null, int? projectId = null, int? projectIndex = null, int? contextId = null, int? contextIndex = null,
                bool starred = false, DateTime? dueDate = null, int? todayIndex = null, bool completed = false, DateTime? dateCompleted = null) => Tasks.Add(new TaskModel
            {
                Id = id, UserId = UserId, TaskName = id.ToString(), InboxIndex = inboxIndex,
                ProjectId = projectId, ProjectName = projectId == null ? null! : $"Project {projectId}", ProjectIndex = projectIndex,
                ContextId = contextId, ContextName = contextId == null ? null! : $"Context {contextId}", ContextIndex = contextIndex,
                Starred = starred, DueDate = dueDate, TodayIndex = todayIndex, Completed = completed, DateCompleted = dateCompleted
            });

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                string path = request.RequestUri!.AbsolutePath;
                switch (request.Method.Method, path)
                {
                    case ("GET", "/api/user/GetCurrentUser"): return JsonResponse(new UserModel { Id = UserId });
                    case ("GET", "/api/user/GetCurrentUserSettings"): return JsonResponse(new UserSettingsModel { CleanUpDelayDays = 7, DeleteDelayDays = 30 });
                    case ("GET", "/api/task/GetAllTasksForUser"): return JsonResponse(Tasks);
                    case ("GET", "/api/project/GetAllProjectsForUser"): return JsonResponse(Projects);
                    case ("GET", "/api/context/GetAllContextsForUser"): return JsonResponse(Contexts);

                    case ("GET", _) when path.StartsWith("/api/task/GetTaskById/", StringComparison.Ordinal):
                        return JsonResponse(Task(int.Parse(path.Split('/')[^1])));
                    case ("GET", _) when path.StartsWith("/api/project/GetProjectById/", StringComparison.Ordinal):
                        return JsonResponse(Projects.Single(x => x.Id == int.Parse(path.Split('/')[^1])));

                    case ("GET", _) when path.StartsWith("/api/context/GetContextById/", StringComparison.Ordinal):
                        return JsonResponse(Contexts.Single(x => x.Id == int.Parse(path.Split('/')[^1])));

                    case ("POST", "/api/task/post"):
                        var added = (await request.Content!.ReadFromJsonAsync<TaskModel>(cancellationToken))!;
                        added.Id = 100;
                        Tasks.Add(added);
                        Writes.Add("POST");
                        return JsonResponse(added);

                    case ("PUT", "/api/task/put"):
                        var sent = (await request.Content!.ReadFromJsonAsync<TaskModel>(cancellationToken))!;
                        Tasks[Tasks.FindIndex(x => x.Id == sent.Id)] = sent;
                        Writes.Add($"PUT {sent.Id}");
                        return new HttpResponseMessage(HttpStatusCode.OK);

                    case ("DELETE", "/api/task/delete"):
                        var deleted = (await request.Content!.ReadFromJsonAsync<TaskModel>(cancellationToken))!;
                        Tasks.RemoveAll(x => x.Id == deleted.Id);
                        Writes.Add($"DELETE {deleted.Id}");
                        return new HttpResponseMessage(HttpStatusCode.OK);

                    case ("DELETE", "/api/project/delete"):
                        var project = (await request.Content!.ReadFromJsonAsync<ProjectModel>(cancellationToken))!;
                        Projects.RemoveAll(x => x.Id == project.Id);
                        return new HttpResponseMessage(HttpStatusCode.OK);

                    case ("DELETE", "/api/context/delete"):
                        var context = (await request.Content!.ReadFromJsonAsync<ContextModel>(cancellationToken))!;
                        Contexts.RemoveAll(x => x.Id == context.Id);
                        return new HttpResponseMessage(HttpStatusCode.OK);

                    case ("PUT", "/api/project/put"):
                        var changed = (await request.Content!.ReadFromJsonAsync<ProjectModel>(cancellationToken))!;
                        Projects[Projects.FindIndex(x => x.Id == changed.Id)] = changed;
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
