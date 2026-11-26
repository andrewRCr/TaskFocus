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
            Assert.Equal(2, _client.Data.GetDataStateLastSyncResult().NumRowsUpdated); // The Settings page reports both.

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
        public async Task SigningInAgainLoadsTheSavedNameAndSettingsAndTheNextChangeIsSaved()
        {
            await _client.Sync(); // Signing in.
            _client.ChangeSettings(x => x.DeleteDelayDays = 60);
            _client.ChangeName("New", "Name");
            await _client.Sync();

            var later = new Client(_api);
            await later.Sync();

            Assert.Equal(60, later.Data.GetDataStateUserSettings()!.DeleteDelayDays);
            Assert.Equal(("New", "Name"), (later.Data.GetDataStateCurrentUser()!.FirstName, later.Data.GetDataStateCurrentUser()!.LastName));

            later.ChangeSettings(x => x.DeleteDelayDays = 30);
            later.ChangeName("Newer", "Name");
            await later.Sync();
            Assert.Equal((30, "Newer"), (_api.Settings.DeleteDelayDays, _api.User.FirstName));
        }

        [Fact]
        public async Task ChangesSavedOnAnotherDeviceAreShownHereAfterTheNextSyncAndKeptByLaterChanges()
        {
            var other = new Client(_api);
            await _client.Sync(); // Signing in on both.
            await other.Sync();

            other.ChangeSettings(x => x.DeleteDelayDays = 90);
            other.ChangeName("Elsewhere", "Name");
            await other.Sync();
            await _client.Sync();

            Assert.Equal(90, _client.Data.GetDataStateUserSettings()!.DeleteDelayDays);
            Assert.Equal("Elsewhere", _client.Data.GetDataStateCurrentUser()!.FirstName);

            _client.ChangeSettings(x => x.CleanUpDelayDays = 3);
            _client.ChangeName("Elsewhere", "Changed");
            await _client.Sync();

            Assert.Equal((90, 3), (_api.Settings.DeleteDelayDays, _api.Settings.CleanUpDelayDays));
            Assert.Equal(("Elsewhere", "Changed"), (_api.User.FirstName, _api.User.LastName));
        }

        // The later edit wins: another device saved its change after the one made here.
        [Fact]
        public async Task AChangeSavedOnAnotherDeviceAfterOneMadeHereIsKeptAndShownHere()
        {
            await _client.Sync(); // Signing in.

            _client.ChangeSettings(x => x.DeleteDelayDays = 60);
            _client.ChangeName("Here", "Name");
            _client.RenameTask(1, "Renamed");
            _api.SaveElsewhere(deleteDelayDays: 90, firstName: "Elsewhere");
            await _client.Sync();

            Assert.Equal(["PUT task 1"], _api.Writes);
            Assert.Equal((90, "Elsewhere"), (_api.Settings.DeleteDelayDays, _api.User.FirstName));
            Assert.Equal(90, _client.Data.GetDataStateUserSettings()!.DeleteDelayDays);
            Assert.Equal("Elsewhere", _client.Data.GetDataStateCurrentUser()!.FirstName);
            Assert.Equal("Renamed", _api.StoredTask.TaskName);
            Assert.Equal(ESyncStatus.Synchronized, _client.Data.GetCurrentSyncStatus());
        }

        // The request never reaches the server; signing out and back in sends the change again.
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task AChangeWhoseSaveFailsIsSavedAfterSigningInAgainAndKeptByTheNextChange(bool name)
        {
            await _client.Sync(); // Signing in.
            Change(name, 1);
            _api.FailNextSave = Failure.Unreachable;
            await _client.SyncFails();

            await _client.SignOutAndIn();
            await _client.Sync();

            Assert.Equal(1, Saved(name));
            Assert.Equal(1, Shown(name));

            ChangeSibling(name);
            await _client.Sync();
            Assert.Equal(1, Saved(name));
            Assert.True(name ? _api.User.LastName == "Sibling" : _api.Settings.CleanUpDelayDays == 3);
        }

        // The server saves the change but its reply is lost, and another change follows it.
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task AChangeMadeAfterASaveWhoseReplyIsLostIsSaved(bool name)
        {
            await _client.Sync(); // Signing in.
            Change(name, 1);
            _api.FailNextSave = Failure.ReplyLost;
            await _client.SyncFails();
            Assert.Equal(1, Saved(name));

            Change(name, 2);
            await _client.SignOutAndIn();
            await _client.Sync();

            Assert.Equal(2, Saved(name));
            Assert.Equal(2, Shown(name));
        }

        public void Dispose() => _api.Dispose();

        private static readonly string[] Names = ["Old", "First", "Second"];
        private static readonly int[] Delays = [30, 60, 45];

        private void Change(bool name, int n)
        {
            if (name) _client.ChangeName(Names[n], "Name");
            else _client.ChangeSettings(x => x.DeleteDelayDays = Delays[n]);
        }

        private void ChangeSibling(bool name)
        {
            if (name) _client.ChangeName(_client.Data.GetDataStateCurrentUser()!.FirstName!, "Sibling");
            else _client.ChangeSettings(x => x.CleanUpDelayDays = 3);
        }

        private int Saved(bool name) => name ? Array.IndexOf(Names, _api.User.FirstName) : Array.IndexOf(Delays, _api.Settings.DeleteDelayDays);

        private int Shown(bool name) => name
            ? Array.IndexOf(Names, _client.Data.GetDataStateCurrentUser()!.FirstName)
            : Array.IndexOf(Delays, _client.Data.GetDataStateUserSettings()!.DeleteDelayDays);

        private enum Failure { None, Unreachable, ReplyLost }

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

            public async Task SyncFails() => Assert.False(await _sync.TrySync());

            // As the Logout page does once its sync has run (or failed), then signing in again.
            public async Task SignOutAndIn()
            {
                Data.ResetDataStateOnLogout();
                await Sync();
            }

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
            public Failure FailNextSave { get; set; }
            public UserModel User { get; } = new() { Id = UserId, Email = "user@example.invalid", FirstName = "Old", LastName = "Name", ServerLastUpdated = Registered, ClientLastUpdated = Registered };
            public UserSettingsModel Settings { get; } = new() { Id = UserId, ServerLastUpdated = Registered, ClientLastUpdated = Registered };
            public TaskModel StoredTask { get; private set; } = new() { Id = 1, UserId = UserId, TaskName = "Task", InboxIndex = 0, ServerLastUpdated = Registered, ClientLastUpdated = Registered };

            // Another device saves a change a moment after this one's.
            public void SaveElsewhere(int deleteDelayDays, string firstName)
            {
                var at = DateTimeOffset.Now.AddSeconds(1);
                (Settings.DeleteDelayDays, Settings.ClientLastUpdated, Settings.ServerLastUpdated) = (deleteDelayDays, at, at);
                (User.FirstName, User.ClientLastUpdated, User.ServerLastUpdated) = (firstName, at, at);
            }

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
                        return Save(() =>
                        {
                            (User.FirstName, User.LastName) = (name.FirstName, name.LastName);
                            (User.ServerLastUpdated, User.ClientLastUpdated) = (name.ServerLastUpdated, name.ClientLastUpdated);
                            Writes.Add("PUT name");
                        });

                    case ("PUT", "/api/user/put/settings"):
                        var settings = (await request.Content!.ReadFromJsonAsync<UserSettingsModel>(cancellationToken))!;
                        if (settings.CleanUpDelayDays is < 1 or > 90 || settings.DeleteDelayDays is < 1 or > 90)
                        {
                            return new HttpResponseMessage(HttpStatusCode.BadRequest);
                        }
                        return Save(() =>
                        {
                            (Settings.CleanUpImmediately, Settings.CleanUpDelayDays, Settings.DeleteDelayDays) =
                                (settings.CleanUpImmediately, settings.CleanUpDelayDays, settings.DeleteDelayDays);
                            (Settings.ServerLastUpdated, Settings.ClientLastUpdated) = (settings.ServerLastUpdated, settings.ClientLastUpdated);
                            Writes.Add("PUT settings");
                        });

                    case ("PUT", "/api/task/put"):
                        StoredTask = (await request.Content!.ReadFromJsonAsync<TaskModel>(cancellationToken))!;
                        Writes.Add($"PUT task {StoredTask.Id}");
                        return new HttpResponseMessage(HttpStatusCode.OK);

                    default:
                        throw new InvalidOperationException($"Unexpected request {request.Method} {path}");
                }
            }

            private HttpResponseMessage Save(Action store)
            {
                var failure = FailNextSave;
                FailNextSave = Failure.None;
                if (failure == Failure.Unreachable) throw new HttpRequestException("unreachable");
                store();
                if (failure == Failure.ReplyLost) throw new HttpRequestException("reply lost");
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
