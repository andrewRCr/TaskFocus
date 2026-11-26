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
    public sealed class CompletedTaskCleanupTests : IDisposable
    {
        private readonly TaskApi _api = new();
        private readonly DataState _state = new();
        private readonly DataService _data;
        private readonly DataSyncService _sync;

        public CompletedTaskCleanupTests()
        {
            IMapper mapper = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<UserModel, UserDisplayModel>().ReverseMap();
                cfg.CreateMap<UserSettingsModel, UserSettingsDisplayModel>().ReverseMap();
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

        private async Task Sync() => Assert.True(await _sync.TrySync(onFailure: e => Assert.Fail(e.ToString())));

        private async Task SyncFails() => Assert.False(await _sync.TrySync());

        // As the Logout page does once its sync has run (or failed), then signing in again.
        private async Task SignOutAndIn()
        {
            _data.ResetDataStateOnLogout();
            await Sync();
        }

        private void ChangeSettings(Action<UserSettingsDisplayModel> change)
        {
            var settings = _data.GetDataStateUserSettings()!;
            change(settings);
            _data.UpdateSettingsData(settings);
        }

        private void Edit(int id, Action<TaskDisplayModel> change)
        {
            var task = _data.GetDataStateTasks()!.Single(x => x.Id == id);
            change(task);
            _data.UpdateTaskData(task);
        }

        // Time passes: the task was completed that many days ago, on the server and here.
        private void Age(int id, int days)
        {
            var completed = DateTime.UtcNow.AddDays(-days);
            _api.Tasks.Single(x => x.Id == id).DateCompleted = completed;
            _state.GetTasks()!.Single(x => x.Id == id).DateCompleted = completed;
            _data.GetDataStateTasks()!.Single(x => x.Id == id).DateCompleted = completed;
        }

        private IEnumerable<string> Deletes => _api.Writes.Where(x => x.StartsWith("DELETE"));

        // The settings delete a completed task after 30 days and clean it up after 7.
        [Theory]
        [InlineData(1)] // only expired tasks, so the last row is also removed
        [InlineData(2)]
        public async Task SigningInKeepsExpiredTasksAndTheFirstSyncDeletesThem(int expired)
        {
            for (int id = 1; id <= expired; id++) _api.Add(id, daysSinceCompleted: 40);
            _api.Add(10, daysSinceCompleted: null);
            _api.Add(11, daysSinceCompleted: 30);
            _api.Add(12, daysSinceCompleted: 1);
            int[] expiredIds = [.. Enumerable.Range(1, expired)];

            await Sync(); // Signing in.

            Assert.Equal(1, _api.Loads); // The first attempt succeeded.
            Assert.Equal(ESyncStatus.Synchronized, _data.GetCurrentSyncStatus());
            Assert.Equal([.. expiredIds, 10, 11, 12], _data.GetDataStateTasks()!.Select(x => x.Id!.Value));
            Assert.DoesNotContain(_state.GetChangedTaskData(), x => x.Deleted.HasValue);
            Assert.Empty(_api.Writes);

            await Sync();

            Assert.Equal(["PUT 11", .. expiredIds.Select(id => $"DELETE {id}")], _api.Writes); // Only task 11 is due to be cleaned up.
            Assert.Equal([10, 11, 12], _api.Tasks.Select(x => x.Id!.Value));
            Assert.Equal([10, 11, 12], _data.GetDataStateTasks()!.Select(x => x.Id!.Value));
            Assert.True(_api.Tasks.Single(x => x.Id == 11).CleanedUp);
            Assert.Empty(_state.GetChangedTaskData());
            Assert.Equal(expired, _data.GetDataStateLastSyncResult().NumRowsDeleted);
            Assert.Equal(ESyncStatus.Synchronized, _data.GetCurrentSyncStatus());
        }

        [Fact]
        public async Task ATaskThatExpiresDuringASessionIsDeletedByTheNextSync()
        {
            _api.Add(1, daysSinceCompleted: 1);
            _api.Add(2, daysSinceCompleted: null);
            await Sync(); // Signing in.

            Age(1, 40);
            await Sync();

            Assert.Equal(["DELETE 1"], _api.Writes);
            Assert.Equal([2], _api.Tasks.Select(x => x.Id!.Value));
            Assert.Equal([2], _data.GetDataStateTasks()!.Select(x => x.Id!.Value));
            Assert.Equal(ESyncStatus.Synchronized, _data.GetCurrentSyncStatus());
        }

        // The delay counts whole days in UTC: a task is deleted once more than the delay has passed, in any time zone.
        [Fact]
        public async Task ATaskIsDeletedOnlyAfterMoreThanTheDelayInEveryTimeZone()
        {
            _api.Add(1, hoursSinceCompleted: 31 * 24 - 1);
            _api.Add(2, hoursSinceCompleted: 31 * 24 + 1);
            _api.Add(3, daysSinceCompleted: null);

            await Sync(); // Signing in.
            await Sync();

            Assert.Equal(["DELETE 2"], Deletes);
            Assert.Equal([1, 3], _api.Tasks.Select(x => x.Id!.Value));
        }

        [Fact]
        public async Task ALongerDelaySetRightAfterSigningInKeepsATaskPastTheOldOne()
        {
            _api.Add(1, daysSinceCompleted: 40);
            await Sync(); // Signing in.

            ChangeSettings(x => x.DeleteDelayDays = 90);
            await Sync();
            await Sync();

            Assert.Empty(Deletes);
            Assert.Equal(90, _api.Settings.DeleteDelayDays);
            Assert.Single(_api.Tasks);
        }

        [Fact]
        public async Task ALongerDelaySavedOnAnotherDeviceKeepsATaskPastTheOldOne()
        {
            _api.Add(1, daysSinceCompleted: 40);
            await Sync(); // Signing in.

            _api.SaveSettingsElsewhere(x => x.DeleteDelayDays = 90);
            await Sync();
            await Sync();

            Assert.Empty(Deletes);
            Assert.Equal(90, _data.GetDataStateUserSettings()!.DeleteDelayDays);
            Assert.Single(_api.Tasks);
        }

        [Fact]
        public async Task AShorterDelayDeletesOnceItIsSaved()
        {
            _api.Add(1, daysSinceCompleted: 5);
            _api.Add(2, daysSinceCompleted: null);
            await Sync(); // Signing in.

            ChangeSettings(x => x.DeleteDelayDays = 1);
            await Sync();

            Assert.Equal(["PUT settings", "DELETE 1"], _api.Writes);
            Assert.Equal([2], _api.Tasks.Select(x => x.Id!.Value));
        }

        // Another device saved its settings after these were changed here, so the server keeps them.
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShorterDelaysThatLoseToALaterChangeElsewhereDeleteAndCleanUpNothing(bool deleteDelay)
        {
            _api.Add(1, daysSinceCompleted: 5);
            await Sync(); // Signing in.

            if (deleteDelay) ChangeSettings(x => x.DeleteDelayDays = 1);
            else ChangeSettings(x => x.CleanUpImmediately = true);
            _api.SaveSettingsElsewhere(x => { x.DeleteDelayDays = 90; x.CleanUpDelayDays = 14; });
            await Sync();
            await Sync();

            Assert.Empty(_api.Writes);
            Assert.False(_api.Tasks.Single().CleanedUp);
            Assert.Equal((90, 14, false), (_data.GetDataStateUserSettings()!.DeleteDelayDays, _data.GetDataStateUserSettings()!.CleanUpDelayDays, _data.GetDataStateUserSettings()!.CleanUpImmediately));
        }

        // As an emptied desktop field gives; that 0 is never saved, and the page's copy keeps it until it is replaced.
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task AnEmptiedDelayFieldDeletesAndCleansUpNothing(bool deleteDelay)
        {
            _api.Add(1, daysSinceCompleted: 2);
            await Sync(); // Signing in.

            if (deleteDelay) ChangeSettings(x => x.DeleteDelayDays = 0);
            else ChangeSettings(x => x.CleanUpDelayDays = 0);
            await Sync();
            await Sync();

            Assert.Empty(_api.Writes);
            Assert.False(_api.Tasks.Single().CleanedUp);
        }

        // Another device reopened the task, but its clock is behind, so the change is stamped before this device's last
        // sync and isn't pulled; this device still holds the task as completed long ago.
        [Fact]
        public async Task ATaskReopenedOnAnotherDeviceIsNotDeletedWhenItsChangeIsNotPulled()
        {
            _api.Add(1, daysSinceCompleted: 40);
            await Sync(); // Signing in.

            var row = _api.Tasks.Single();
            (row.Completed, row.DateCompleted, row.ServerLastUpdated) = (false, null, DateTimeOffset.Now.AddHours(-1));
            await Sync();

            Assert.Empty(Deletes);
            Assert.Single(_api.Tasks);
        }

        [Fact]
        public async Task ATaskCompletedAgainIsKeptForTheWholeDelayFromThen()
        {
            _api.Add(1, daysSinceCompleted: 30);
            await Sync(); // Signing in.
            Edit(1, x => x.Completed = false);
            await Sync();

            ChangeSettings(x => x.DeleteDelayDays = 1);
            Edit(1, x => x.Completed = true);
            await Sync();
            await Sync();

            Assert.Empty(Deletes);
            Assert.True(_api.Tasks.Single().Completed);
        }

        [Fact]
        public async Task AnExpiredTaskThatWasAlreadyCleanedUpIsDeletedEvenWhenItIsTheOnlyOne()
        {
            _api.Add(1, daysSinceCompleted: 40, cleanedUp: true);

            await Sync(); // Signing in.
            await Sync();

            Assert.Equal(["DELETE 1"], _api.Writes);
            Assert.Empty(_api.Tasks);
            Assert.Empty(_data.GetDataStateTasks()!);
        }

        // The reopen's save fails; after signing in again, the loaded row still shows the task completed.
        [Fact]
        public async Task AReopenedTaskIsNotDeletedWhenItsSaveFailsAndItIsSentAfterSigningInAgain()
        {
            _api.Settings.DeleteDelayDays = 90;
            _api.Add(1, daysSinceCompleted: 40);
            _api.Add(2, daysSinceCompleted: null);
            await Sync(); // Signing in.

            Edit(1, x => x.Completed = false);
            ChangeSettings(x => x.DeleteDelayDays = 30);
            _api.FailNext = "PUT 1";
            await SyncFails();
            Assert.Equal(30, _api.Settings.DeleteDelayDays);

            await SignOutAndIn();
            await Sync();
            await Sync();

            Assert.Empty(Deletes);
            Assert.False(_api.Tasks.Single(x => x.Id == 1).Completed);
        }

        // Deleting an expired task fails once; nothing about it is queued, so other changes are sent once.
        [Fact]
        public async Task AFailedDeleteIsTriedAgainLaterAndOtherChangesAreSentOnce()
        {
            _api.Add(1, daysSinceCompleted: 40);
            await Sync(); // Signing in.

            _data.AddTask(new TaskDisplayModel { TaskName = "New" });
            _api.FailNext = "DELETE 1";
            await SyncFails();
            await SignOutAndIn();
            await Sync();

            Assert.Equal(["New"], _api.Tasks.Select(x => x.TaskName));
            Assert.Equal(["New"], _data.GetDataStateTasks()!.Select(x => x.TaskName));
        }

        [Fact]
        public async Task AnEditMadeWhileAnExpiredTaskIsBeingDeletedIsSentByTheNextSync()
        {
            _api.Add(1, daysSinceCompleted: 40);
            _api.Add(2, daysSinceCompleted: null);
            await Sync(); // Signing in.

            _api.OnDelete = () => Edit(2, x => x.TaskName = "Renamed");
            await Sync();
            await Sync();

            Assert.Equal(["DELETE 1", "PUT 2"], _api.Writes);
            Assert.Equal("Renamed", _api.Tasks.Single().TaskName);
        }

        [Fact]
        public async Task ADelayShortenedDuringASyncDeletesOnlyOnceItIsSaved()
        {
            _api.Add(1, daysSinceCompleted: 5);
            await Sync(); // Signing in.

            _api.OnPull = () => ChangeSettings(x => x.DeleteDelayDays = 1);
            await Sync();
            Assert.Empty(_api.Writes);

            await Sync();
            Assert.Equal(["PUT settings", "DELETE 1"], _api.Writes);
        }

        // Renaming an expired task during a sync: the rename is sent, and the task is deleted after it.
        [Fact]
        public async Task AnExpiredTaskEditedDuringASyncIsDeletedAfterTheEditIsSent()
        {
            _api.Add(1, daysSinceCompleted: 40);
            await Sync(); // Signing in.

            _api.OnPull = () => Edit(1, x => x.TaskName = "Renamed");
            await Sync();
            await Sync();

            Assert.Equal(["PUT 1", "DELETE 1"], _api.Writes);
            Assert.Equal(ESyncStatus.Synchronized, _data.GetCurrentSyncStatus());
        }

        // After signing in again, the loaded row still shows the task completed (and in Today, as older versions left it).
        [Fact]
        public async Task AReopenWaitingToBeSentIsNotUndoneByCleaningUpAfterSigningInAgain()
        {
            _api.Add(1, daysSinceCompleted: 10, todayIndex: 0);
            await Sync(); // Signing in.

            Edit(1, x => x.Completed = false);
            _api.FailNext = "PUT 1";
            await SyncFails();

            await SignOutAndIn();
            await Sync();

            var saved = _api.Tasks.Single();
            Assert.False(saved.Completed);
            Assert.False(saved.CleanedUp);
        }

        public void Dispose() => _api.Dispose();

        private sealed class TaskApi : HttpMessageHandler, IAPIHelper
        {
            private const string UserId = "test-user";

            public TaskApi() => APIClient = new HttpClient(this, disposeHandler: false) { BaseAddress = new Uri("https://taskfocus.test") };

            public HttpClient APIClient { get; }
            public List<TaskModel> Tasks { get; } = [];
            public List<string> Writes { get; } = [];
            public int Loads { get; private set; }

            public UserSettingsModel Settings { get; } = new() { DeleteDelayDays = 30, CleanUpDelayDays = 7 };
            public string? FailNext { get; set; } // e.g. "PUT 1": that request fails once, before reaching the server
            public Action? OnDelete { get; set; } // runs while a delete is awaiting its reply
            public Action? OnPull { get; set; } // runs while a sync is pulling the tasks
            private int _nextId = 100;

            // Another device saves settings a moment after this one's change.
            public void SaveSettingsElsewhere(Action<UserSettingsModel> change)
            {
                change(Settings);
                Settings.ClientLastUpdated = Settings.ServerLastUpdated = DateTimeOffset.Now.AddSeconds(1);
            }

            // The server stamps completions in UTC.
            public void Add(int id, int? daysSinceCompleted = null, int? hoursSinceCompleted = null, bool cleanedUp = false, int? todayIndex = null)
            {
                int? hours = hoursSinceCompleted ?? daysSinceCompleted * 24;
                Tasks.Add(new TaskModel
                {
                    Id = id, UserId = UserId, TaskName = $"Task {id}", InboxIndex = cleanedUp ? null : Tasks.Count,
                    Completed = hours.HasValue, CleanedUp = cleanedUp, TodayIndex = todayIndex, Starred = todayIndex != null,
                    DateCompleted = hours.HasValue ? DateTime.UtcNow.AddHours(-hours.Value) : null
                });
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                string path = request.RequestUri!.AbsolutePath;
                switch (request.Method.Method, path)
                {
                    case ("GET", "/api/user/GetCurrentUser"):
                        Loads++;
                        return JsonResponse(new UserModel { Id = UserId });

                    case ("GET", "/api/user/GetCurrentUserSettings"):
                        return JsonResponse(Settings);

                    case ("PUT", "/api/user/put/settings"):
                        var changed = (await request.Content!.ReadFromJsonAsync<UserSettingsModel>(cancellationToken))!;
                        (Settings.DeleteDelayDays, Settings.CleanUpDelayDays, Settings.CleanUpImmediately) = (changed.DeleteDelayDays, changed.CleanUpDelayDays, changed.CleanUpImmediately);
                        (Settings.ClientLastUpdated, Settings.ServerLastUpdated) = (changed.ClientLastUpdated, changed.ServerLastUpdated);
                        Writes.Add("PUT settings");
                        return new HttpResponseMessage(HttpStatusCode.OK);

                    case ("GET", "/api/task/GetAllTasksForUser"):
                        OnPull?.Invoke();
                        OnPull = null;
                        return JsonResponse(Tasks);

                    case ("GET", "/api/project/GetAllProjectsForUser"):
                        return JsonResponse(Array.Empty<ProjectModel>());

                    case ("GET", "/api/context/GetAllContextsForUser"):
                        return JsonResponse(Array.Empty<ContextModel>());

                    case ("GET", _) when path.StartsWith("/api/task/GetTaskById/", StringComparison.Ordinal):
                        var found = Tasks.SingleOrDefault(x => x.Id == int.Parse(path.Split('/')[^1]));
                        return found == null ? new HttpResponseMessage(HttpStatusCode.NoContent) : JsonResponse(found);

                    case ("POST", "/api/task/post"):
                        var added = (await request.Content!.ReadFromJsonAsync<TaskModel>(cancellationToken))!;
                        added.Id = _nextId++;
                        added.UserId = UserId;
                        added.DateCompleted = added.Completed ? DateTime.UtcNow : null;
                        Tasks.Add(added);
                        Writes.Add($"POST {added.Id}");
                        return JsonResponse(added);

                    case ("PUT", "/api/task/put"):
                    case ("DELETE", "/api/task/delete"):
                        var sent = (await request.Content!.ReadFromJsonAsync<TaskModel>(cancellationToken))!;
                        string write = $"{request.Method} {sent.Id}";
                        if (FailNext == write) { FailNext = null; throw new HttpRequestException("unreachable"); }
                        int index = Tasks.FindIndex(x => x.Id == sent.Id);
                        Writes.Add(write);
                        if (request.Method == HttpMethod.Put)
                        {
                            // As the API does: it records completion itself, and reopening clears it and the clean-up.
                            var stored = Tasks[index];
                            sent.DateCompleted = !sent.Completed ? null : stored.Completed ? stored.DateCompleted : DateTime.UtcNow;
                            if (!sent.Completed) sent.CleanedUp = false;
                            Tasks[index] = sent;
                        }
                        else
                        {
                            OnDelete?.Invoke();
                            OnDelete = null;
                            if (index >= 0) Tasks.RemoveAt(index); // The API answers OK when the task is already gone.
                        }
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
