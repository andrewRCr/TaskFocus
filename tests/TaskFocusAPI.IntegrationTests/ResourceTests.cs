using System.Net;
using System.Net.Http.Json;
using TaskFocusAPI.Library.Models;

namespace TaskFocusAPI.IntegrationTests
{
    [Collection(ApiCollection.Name)]
    public sealed class ResourceTests(ApiFixture fixture)
    {
        private const string Tasks = "/api/Task/GetAllTasksForUser";
        private const string Projects = "/api/Project/GetAllProjectsForUser";
        private const string Contexts = "/api/Context/GetAllContextsForUser";

        private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

        [Fact]
        public async Task TaskRoundTripsThroughCreateReadUpdateAndDelete()
        {
            using var client = await fixture.CreateAuthenticatedClientAsync();
            var task = await CreateTaskAsync(client, "Original task");
            var saved = await GetAsync<TaskModel>(client, $"/api/Task/GetTaskById/{task.Id}");
            Assert.Equal(task.Id, saved.Id);
            Assert.Equal("Original task", saved.TaskName);

            task.TaskName = "Updated task";
            task.Starred = true;
            await UpdateAsync(client, "Task", task);
            saved = await GetAsync<TaskModel>(client, $"/api/Task/GetTaskById/{task.Id}");
            Assert.Equal(task.TaskName, saved.TaskName);
            Assert.True(saved.Starred);

            await DeleteAsync(client, "Task", task);
            Assert.Empty(await GetAsync<List<TaskModel>>(client, Tasks));
        }

        [Fact]
        public async Task ProjectRoundTripsThroughCreateReadUpdateAndDelete()
        {
            using var client = await fixture.CreateAuthenticatedClientAsync();
            var project = await CreateProjectAsync(client, "Original project");
            var saved = await GetAsync<ProjectModel>(client, $"/api/Project/GetProjectById/{project.Id}");
            Assert.Equal(project.Id, saved.Id);
            Assert.Equal("Original project", saved.ProjectName);

            project.ProjectName = "Updated project";
            project.OrderIndex = 3;
            await UpdateAsync(client, "Project", project);
            saved = await GetAsync<ProjectModel>(client, $"/api/Project/GetProjectById/{project.Id}");
            Assert.Equal(project.ProjectName, saved.ProjectName);
            Assert.Equal(project.OrderIndex, saved.OrderIndex);

            await DeleteAsync(client, "Project", project);
            Assert.Empty(await GetAsync<List<ProjectModel>>(client, Projects));
        }

        [Fact]
        public async Task ContextRoundTripsThroughCreateReadUpdateAndDelete()
        {
            using var client = await fixture.CreateAuthenticatedClientAsync();
            var context = await CreateContextAsync(client, "Original context");
            var saved = await GetAsync<ContextModel>(client, $"/api/Context/GetContextById/{context.Id}");
            Assert.Equal(context.Id, saved.Id);
            Assert.Equal("Original context", saved.ContextName);

            context.ContextName = "Updated context";
            context.OrderIndex = 2;
            await UpdateAsync(client, "Context", context);
            saved = await GetAsync<ContextModel>(client, $"/api/Context/GetContextById/{context.Id}");
            Assert.Equal(context.ContextName, saved.ContextName);
            Assert.Equal(context.OrderIndex, saved.OrderIndex);

            await DeleteAsync(client, "Context", context);
            Assert.Empty(await GetAsync<List<ContextModel>>(client, Contexts));
        }

        [Theory]
        [InlineData(Tasks)]
        [InlineData("/api/Task/GetInboxTasksForUser")]
        public async Task TaskListsContainOnlyTheSignedInUsersTasks(string route)
        {
            using var first = await fixture.CreateAuthenticatedClientAsync();
            using var second = await fixture.CreateAuthenticatedClientAsync();
            var firstTask = await CreateTaskAsync(first, "First user's task");
            var secondTask = await CreateTaskAsync(second, "Second user's task");

            Assert.Equal(firstTask.Id, Assert.Single(await GetAsync<List<TaskModel>>(first, route)).Id);
            Assert.Equal(secondTask.Id, Assert.Single(await GetAsync<List<TaskModel>>(second, route)).Id);
        }

        [Fact]
        public async Task ProjectListContainsOnlyTheSignedInUsersProjects()
        {
            using var first = await fixture.CreateAuthenticatedClientAsync();
            using var second = await fixture.CreateAuthenticatedClientAsync();
            var firstProject = await CreateProjectAsync(first, "First user's project");
            var secondProject = await CreateProjectAsync(second, "Second user's project");

            Assert.Equal(firstProject.Id, Assert.Single(await GetAsync<List<ProjectModel>>(first, Projects)).Id);
            Assert.Equal(secondProject.Id, Assert.Single(await GetAsync<List<ProjectModel>>(second, Projects)).Id);
        }

        [Fact]
        public async Task ContextListContainsOnlyTheSignedInUsersContexts()
        {
            using var first = await fixture.CreateAuthenticatedClientAsync();
            using var second = await fixture.CreateAuthenticatedClientAsync();
            var firstContext = await CreateContextAsync(first, "First user's context");
            var secondContext = await CreateContextAsync(second, "Second user's context");

            Assert.Equal(firstContext.Id, Assert.Single(await GetAsync<List<ContextModel>>(first, Contexts)).Id);
            Assert.Equal(secondContext.Id, Assert.Single(await GetAsync<List<ContextModel>>(second, Contexts)).Id);
        }

        [Fact]
        public async Task TaskUpdatePersistsServerLastUpdatedForSync()
        {
            using var client = await fixture.CreateAuthenticatedClientAsync();
            var task = await CreateTaskAsync(client, "Sync task");
            var newTimestamp = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
            Assert.NotEqual(newTimestamp, task.ServerLastUpdated);

            task.ServerLastUpdated = newTimestamp;
            await UpdateAsync(client, "Task", task);

            var listedTask = Assert.Single(await GetAsync<List<TaskModel>>(client, Tasks));
            Assert.Equal(newTimestamp, listedTask.ServerLastUpdated);
        }

        private static async Task<TaskModel> CreateTaskAsync(HttpClient client, string name)
        {
            using var response = await HttpClientJsonExtensions.PostAsJsonAsync(client, "/api/Task/Post", new TaskModel { TaskName = name }, CancellationToken);
            return await ReadCreatedAsync<TaskModel>(response);
        }

        private static async Task<ProjectModel> CreateProjectAsync(HttpClient client, string name)
        {
            using var response = await HttpClientJsonExtensions.PostAsJsonAsync(client, "/api/Project/Post", new ProjectModel { ProjectName = name }, CancellationToken);
            return await ReadCreatedAsync<ProjectModel>(response);
        }

        private static async Task<ContextModel> CreateContextAsync(HttpClient client, string name)
        {
            using var response = await HttpClientJsonExtensions.PostAsJsonAsync(client, "/api/Context/Post", new ContextModel { ContextName = name }, CancellationToken);
            response.EnsureSuccessStatusCode();
            return Assert.Single(await GetAsync<List<ContextModel>>(client, Contexts));
        }

        private static async Task<T> ReadCreatedAsync<T>(HttpResponseMessage response) where T : class
        {
            response.EnsureSuccessStatusCode();
            var item = await response.Content.ReadFromJsonAsync<T>(CancellationToken);
            Assert.NotNull(item);
            return item;
        }

        private static async Task<T> GetAsync<T>(HttpClient client, string route)
        {
            var item = await client.GetFromJsonAsync<T>(route, CancellationToken);
            Assert.NotNull(item);
            return item;
        }

        private static async Task UpdateAsync<T>(HttpClient client, string resource, T item)
        {
            using var response = await HttpClientJsonExtensions.PutAsJsonAsync(client, $"/api/{resource}/Put", item, CancellationToken);
            response.EnsureSuccessStatusCode();
        }

        private static async Task DeleteAsync<T>(HttpClient client, string resource, T item)
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/{resource}/Delete")
            {
                Content = JsonContent.Create(item)
            };
            using var response = await client.SendAsync(request, CancellationToken);
            response.EnsureSuccessStatusCode();
        }
    }
}
