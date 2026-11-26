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
    public sealed class ProfileAndSettingsSyncTests : IDisposable
    {
        private readonly AccountApi _api = new();
        private readonly Client _client;

        public ProfileAndSettingsSyncTests() => _client = new Client(_api);

        [Fact]
        public async Task ChangingASettingSavesItAndTheTaskEditsQueuedWithIt()
        {
            await _client.Sync(); // Signing in.

            _client.ChangeSettings(x => { x.DeleteDelayDays = 60; x.CleanUpDelayDays = 3; x.CleanUpImmediately = true; });
            _client.RenameTask(1, "Renamed");
            await _client.Sync();

            Assert.Equal(["PUT settings", "PUT task 1"], _api.Writes);
            Assert.Equal((60, 3, true), (_api.Settings.DeleteDelayDays, _api.Settings.CleanUpDelayDays, _api.Settings.CleanUpImmediately));
            Assert.Equal("Renamed", _api.StoredTask.TaskName);
            Assert.Equal(ESyncStatus.Synchronized, _client.Data.GetCurrentSyncStatus());

            await _client.Sync();
            Assert.Equal(2, _api.Writes.Count); // Nothing is sent again.
        }

        [Fact]
        public async Task ChangingTheNameSavesItAndTheTaskEditsQueuedWithIt()
        {
            await _client.Sync(); // Signing in.

            _client.ChangeName("New", "Name");
            _client.RenameTask(1, "Renamed");
            await _client.Sync();

            Assert.Equal(["PUT name", "PUT task 1"], _api.Writes);
            Assert.Equal(("New", "Name"), (_api.User.FirstName, _api.User.LastName));
            Assert.Equal("Renamed", _api.StoredTask.TaskName);
            Assert.Equal(ESyncStatus.Synchronized, _client.Data.GetCurrentSyncStatus());
        }

        [Fact]
        public async Task ASecondChangeInTheSameSessionIsSavedToo()
        {
            await _client.Sync(); // Signing in.

            _client.ChangeSettings(x => x.DeleteDelayDays = 60);
            _client.ChangeName("First", "Change");
            await _client.Sync();
            _client.ChangeSettings(x => x.DeleteDelayDays = 45);
            _client.ChangeName("Second", "Change");
            await _client.Sync();

            Assert.Equal(["PUT name", "PUT settings", "PUT name", "PUT settings"], _api.Writes);
            Assert.Equal(45, _api.Settings.DeleteDelayDays);
            Assert.Equal("Second", _api.User.FirstName);
        }

        [Fact]
        public async Task SigningInAgainLoadsTheSavedNameAndSettings()
        {
            await _client.Sync(); // Signing in.
            _client.ChangeSettings(x => x.DeleteDelayDays = 60);
            _client.ChangeName("New", "Name");
            await _client.Sync();

            var later = new Client(_api);
            await later.Sync();

            Assert.Equal(60, later.Data.GetDataStateUserSettings()!.DeleteDelayDays);
            Assert.Equal(("New", "Name"), (later.Data.GetDataStateCurrentUser()!.FirstName, later.Data.GetDataStateCurrentUser()!.LastName));
        }

        // Another device saved its change after this one last loaded the settings, so the server keeps it.
        [Fact]
        public async Task AChangeSavedOnAnotherDeviceSinceTheLastLoadIsKeptAndSyncKeepsRunning()
        {
            await _client.Sync(); // Signing in.
            _api.Settings.DeleteDelayDays = 90;
            _api.Settings.ClientLastUpdated = _api.Settings.ServerLastUpdated = DateTimeOffset.Now;
            _api.User.FirstName = "Elsewhere";
            _api.User.ClientLastUpdated = _api.User.ServerLastUpdated = DateTimeOffset.Now;

            _client.ChangeSettings(x => x.DeleteDelayDays = 60);
            _client.ChangeName("Here", "Name");
            _client.RenameTask(1, "Renamed");
            await _client.Sync();

            Assert.Equal(90, _api.Settings.DeleteDelayDays);
            Assert.Equal("Elsewhere", _api.User.FirstName);
            Assert.Equal("Renamed", _api.StoredTask.TaskName);
            Assert.Equal(ESyncStatus.Synchronized, _client.Data.GetCurrentSyncStatus());
        }

        public void Dispose() => _api.Dispose();

        private sealed class Client
        {
            private readonly DataSyncService _sync;

            public Client(AccountApi api)
            {
                IMapper mapper = new MapperConfiguration(cfg =>
                {
                    cfg.CreateMap<UserModel, UserDisplayModel>().ReverseMap();
                    cfg.CreateMap<UserSettingsModel, UserSettingsDisplayModel>().ReverseMap();
                    cfg.CreateMap<TaskModel, TaskDisplayModel>().ReverseMap();
                    cfg.CreateMap<ProjectModel, ProjectDisplayModel>().ReverseMap();
                    cfg.CreateMap<ContextModel, ContextDisplayModel>().ReverseMap();
                }).CreateMapper();

                var state = new DataState();
                var helper = new DataHelper(mapper, state);
                var users = new UserEndpoint(api);
                var tasks = new TaskEndpoint(api);
                var projects = new ProjectEndpoint(api);
                var contexts = new ContextEndpoint(api);
                Data = new DataService(mapper, helper, state, users, tasks, projects, contexts, NullLogger<DataService>.Instance);
                _sync = new DataSyncService(mapper, helper, state, users, tasks, projects, contexts, NullLogger<DataSyncService>.Instance, Data);
            }

            public DataService Data { get; }

            public async Task Sync() => Assert.True(await _sync.TrySync(onFailure: e => Assert.Fail(e.ToString())));

            // As the settings page does: it edits the working copy, and each change saves it.
            public void ChangeSettings(Action<UserSettingsDisplayModel> change)
            {
                var settings = Data.GetDataStateUserSettings()!;
                change(settings);
                Data.UpdateSettingsData(settings);
            }

            public void ChangeName(string firstName, string lastName)
            {
                var user = Data.GetDataStateCurrentUser()!;
                (user.FirstName, user.LastName) = (firstName, lastName);
                Data.UpdateUserNameData(user);
            }

            public void RenameTask(int id, string name)
            {
                var task = Data.GetDataStateTasks()!.Single(x => x.Id == id);
                task.TaskName = name;
                Data.UpdateTaskData(task);
            }
        }

        // Stores what the API stores: the edited fields and both timestamps from the request.
        private sealed class AccountApi : HttpMessageHandler, IAPIHelper
        {
            private const string UserId = "test-user";
            private static readonly DateTimeOffset Registered = DateTimeOffset.Now.AddDays(-10);

            public AccountApi() => APIClient = new HttpClient(this, disposeHandler: false) { BaseAddress = new Uri("https://taskfocus.test") };

            public HttpClient APIClient { get; }
            public List<string> Writes { get; } = [];
            public UserModel User { get; } = new() { Id = UserId, Email = "user@example.invalid", FirstName = "Old", LastName = "Name", ServerLastUpdated = Registered, ClientLastUpdated = Registered };
            public UserSettingsModel Settings { get; } = new() { Id = UserId, ServerLastUpdated = Registered, ClientLastUpdated = Registered };
            public TaskModel StoredTask { get; private set; } = new() { Id = 1, UserId = UserId, TaskName = "Task", InboxIndex = 0, ServerLastUpdated = Registered, ClientLastUpdated = Registered };

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                string path = request.RequestUri!.AbsolutePath.ToLowerInvariant();
                switch (request.Method.Method, path)
                {
                    case ("GET", "/api/user/getcurrentuser"):
                        return JsonResponse(User);

                    case ("GET", "/api/user/getcurrentusersettings"):
                        return JsonResponse(Settings);

                    case ("GET", "/api/task/getalltasksforuser"):
                        return JsonResponse(new[] { StoredTask });

                    case ("GET", "/api/project/getallprojectsforuser"):
                        return JsonResponse(Array.Empty<ProjectModel>());

                    case ("GET", "/api/context/getallcontextsforuser"):
                        return JsonResponse(Array.Empty<ContextModel>());

                    case ("GET", "/api/task/gettaskbyid/1"):
                        return JsonResponse(StoredTask);

                    case ("PUT", "/api/user/updatename"):
                        var name = (await request.Content!.ReadFromJsonAsync<UserModel>(cancellationToken))!;
                        (User.FirstName, User.LastName) = (name.FirstName, name.LastName);
                        (User.ServerLastUpdated, User.ClientLastUpdated) = (name.ServerLastUpdated, name.ClientLastUpdated);
                        Writes.Add("PUT name");
                        return new HttpResponseMessage(HttpStatusCode.OK);

                    case ("PUT", "/api/user/put/settings"):
                        var settings = (await request.Content!.ReadFromJsonAsync<UserSettingsModel>(cancellationToken))!;
                        if (settings.CleanUpDelayDays is < 1 or > 90 || settings.DeleteDelayDays is < 1 or > 90)
                        {
                            return new HttpResponseMessage(HttpStatusCode.BadRequest);
                        }
                        (Settings.CleanUpImmediately, Settings.CleanUpDelayDays, Settings.DeleteDelayDays) =
                            (settings.CleanUpImmediately, settings.CleanUpDelayDays, settings.DeleteDelayDays);
                        (Settings.ServerLastUpdated, Settings.ClientLastUpdated) = (settings.ServerLastUpdated, settings.ClientLastUpdated);
                        Writes.Add("PUT settings");
                        return new HttpResponseMessage(HttpStatusCode.OK);

                    case ("PUT", "/api/task/put"):
                        StoredTask = (await request.Content!.ReadFromJsonAsync<TaskModel>(cancellationToken))!;
                        Writes.Add($"PUT task {StoredTask.Id}");
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
