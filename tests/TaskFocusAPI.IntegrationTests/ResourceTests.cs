using System.Data;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Dapper;
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

            await DeleteAsync(client, "Task", new { task.Id });
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

            await DeleteAsync(client, "Project", new { project.Id });
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

            await DeleteAsync(client, "Context", new { context.Id });
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

        [Theory]
        [InlineData("Task", "GET", null)]
        [InlineData("Task", "PUT", "caller")]
        [InlineData("Task", "PUT", "owner")]
        [InlineData("Task", "DELETE", "caller")]
        [InlineData("Task", "DELETE", "owner")]
        [InlineData("Task", "DELETE", null)]
        [InlineData("Project", "GET", null)]
        [InlineData("Project", "PUT", "caller")]
        [InlineData("Project", "PUT", "owner")]
        [InlineData("Project", "DELETE", "caller")]
        [InlineData("Project", "DELETE", "owner")]
        [InlineData("Project", "DELETE", null)]
        [InlineData("Context", "GET", null)]
        [InlineData("Context", "PUT", "caller")]
        [InlineData("Context", "PUT", "owner")]
        [InlineData("Context", "DELETE", "caller")]
        [InlineData("Context", "DELETE", "owner")]
        [InlineData("Context", "DELETE", null)]
        public async Task ForeignResourceBehavesLikeMissingResource(string resource, string method, string? bodyUser)
        {
            var ownerUser = await fixture.CreateUserAsync();
            using var owner = await fixture.CreateAuthenticatedClientAsync(ownerUser);
            var callerUser = await fixture.CreateUserAsync();
            using var caller = await fixture.CreateAuthenticatedClientAsync(callerUser);
            const string originalName = "Owner's item";
            int? id = resource switch
            {
                "Task" => (await CreateTaskAsync(owner, originalName)).Id,
                "Project" => (await CreateProjectAsync(owner, originalName)).Id,
                "Context" => (await CreateContextAsync(owner, originalName)).Id,
                _ => throw new ArgumentOutOfRangeException(nameof(resource))
            };
            Assert.NotNull(id);
            string readRoute = $"/api/{resource}/Get{resource}ById/{id}";
            var original = await GetAsync<JsonElement>(owner, readRoute);

            using var foreignRequest = CreateRequest(id.Value);
            using var foreignResponse = await caller.SendAsync(foreignRequest, CancellationToken);
            var expectedStatus = method switch
            {
                "GET" => HttpStatusCode.NoContent,
                "PUT" => HttpStatusCode.NotFound,
                "DELETE" => HttpStatusCode.OK,
                _ => throw new ArgumentOutOfRangeException(nameof(method))
            };
            Assert.Equal(expectedStatus, foreignResponse.StatusCode);
            Assert.True(JsonElement.DeepEquals(original, await GetAsync<JsonElement>(owner, readRoute)));

            using var missingRequest = CreateRequest(-1);
            using var missingResponse = await caller.SendAsync(missingRequest, CancellationToken);
            Assert.Equal(foreignResponse.StatusCode, missingResponse.StatusCode);
            Assert.Equal(foreignResponse.Content.Headers.ContentType, missingResponse.Content.Headers.ContentType);

            if (method == "PUT")
            {
                var foreignProblem = await foreignResponse.Content.ReadFromJsonAsync<JsonObject>(CancellationToken);
                var missingProblem = await missingResponse.Content.ReadFromJsonAsync<JsonObject>(CancellationToken);
                Assert.NotNull(foreignProblem);
                Assert.NotNull(missingProblem);
                foreignProblem.Remove("traceId");
                missingProblem.Remove("traceId");
                Assert.True(JsonNode.DeepEquals(foreignProblem, missingProblem));
            }
            else
            {
                Assert.Empty(await foreignResponse.Content.ReadAsStringAsync(CancellationToken));
                Assert.Empty(await missingResponse.Content.ReadAsStringAsync(CancellationToken));
            }

            HttpRequestMessage CreateRequest(int itemId)
            {
                if (method == "GET")
                {
                    return new HttpRequestMessage(HttpMethod.Get, $"/api/{resource}/Get{resource}ById/{itemId}");
                }

                string userId = bodyUser == "owner" ? ownerUser.Id : callerUser.Id;
                const string changedName = "Changed name";
                var content = bodyUser == null ? JsonContent.Create(new { Id = itemId }) : resource switch
                {
                    "Task" => JsonContent.Create(new TaskModel { Id = itemId, UserId = userId, TaskName = changedName }),
                    "Project" => JsonContent.Create(new ProjectModel { Id = itemId, UserId = userId, ProjectName = changedName }),
                    "Context" => JsonContent.Create(new ContextModel { Id = itemId, UserId = userId, ContextName = changedName }),
                    _ => throw new ArgumentOutOfRangeException(nameof(resource))
                };
                return new HttpRequestMessage(new HttpMethod(method), $"/api/{resource}/{method}") { Content = content };
            }
        }

        [Theory]
        [InlineData(false, "Project")]
        [InlineData(false, "Context")]
        [InlineData(true, "Project")]
        [InlineData(true, "Context")]
        public async Task TaskWriteRejectsAnotherUsersParentWithoutSaving(bool update, string parent)
        {
            using var client = await fixture.CreateAuthenticatedClientAsync();
            using var otherUser = await fixture.CreateAuthenticatedClientAsync();
            var original = update ? await CreateTaskAsync(client, "Original task") : null;
            var attempted = new TaskModel { Id = original?.Id, TaskName = "Changed task" };
            if (parent == "Project")
            {
                attempted.ProjectId = (await CreateProjectAsync(otherUser, "Other project")).Id;
            }
            else
            {
                attempted.ContextId = (await CreateContextAsync(otherUser, "Other context")).Id;
            }

            using var request = new HttpRequestMessage(update ? HttpMethod.Put : HttpMethod.Post,
                update ? "/api/Task/Put" : "/api/Task/Post") { Content = JsonContent.Create(attempted) };
            using var response = await client.SendAsync(request, CancellationToken);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var tasks = await GetAsync<List<TaskModel>>(client, Tasks);
            if (original == null)
            {
                Assert.Empty(tasks);
            }
            else
            {
                var saved = Assert.Single(tasks);
                Assert.Equal(original.Id, saved.Id);
                Assert.Equal(original.TaskName, saved.TaskName);
                Assert.Null(saved.ProjectId);
                Assert.Null(saved.ContextId);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ProjectWriteRejectsAnotherUsersContextWithoutSaving(bool update)
        {
            using var client = await fixture.CreateAuthenticatedClientAsync();
            using var otherUser = await fixture.CreateAuthenticatedClientAsync();
            var context = await CreateContextAsync(otherUser, "Other context");
            var original = update ? await CreateProjectAsync(client, "Original project") : null;
            var attempted = new ProjectModel { Id = original?.Id, ProjectName = "Changed project", ContextId = context.Id };

            using var request = new HttpRequestMessage(update ? HttpMethod.Put : HttpMethod.Post,
                update ? "/api/Project/Put" : "/api/Project/Post") { Content = JsonContent.Create(attempted) };
            using var response = await client.SendAsync(request, CancellationToken);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var projects = await GetAsync<List<ProjectModel>>(client, Projects);
            if (original == null)
            {
                Assert.Empty(projects);
            }
            else
            {
                var saved = Assert.Single(projects);
                Assert.Equal(original.Id, saved.Id);
                Assert.Equal(original.ProjectName, saved.ProjectName);
                Assert.Null(saved.ContextId);
            }
        }

        [Theory]
        [InlineData(false, "Project")]
        [InlineData(false, "Context")]
        [InlineData(true, "Project")]
        [InlineData(true, "Context")]
        public async Task TaskProceduresRejectAnotherUsersParentWithoutSaving(bool update, string parent)
        {
            var user = await fixture.CreateUserAsync();
            using var client = await fixture.CreateAuthenticatedClientAsync(user);
            using var otherUser = await fixture.CreateAuthenticatedClientAsync();
            var original = update ? await CreateTaskAsync(client, "Original task") : null;
            var attempted = new TaskModel { Id = original?.Id, UserId = user.Id, TaskName = "Changed task" };
            if (parent == "Project")
            {
                attempted.ProjectId = (await CreateProjectAsync(otherUser, "Other project")).Id;
            }
            else
            {
                attempted.ContextId = (await CreateContextAsync(otherUser, "Other context")).Id;
            }

            await using var connection = fixture.CreateDataConnection();
            await connection.OpenAsync(CancellationToken);
            var read = new CommandDefinition("SELECT * FROM dbo.Task WHERE UserId = @UserId",
                new { attempted.UserId }, cancellationToken: CancellationToken);
            var before = await connection.QueryAsync<TaskModel>(read);
            var write = new CommandDefinition(update ? "dbo.spTask_UpdateForUser" : "dbo.spTask_InsertForUser",
                attempted, commandType: CommandType.StoredProcedure, cancellationToken: CancellationToken);

            if (update)
            {
                Assert.Equal(-1, await connection.QuerySingleAsync<int>(write));
            }
            else
            {
                Assert.Empty(await connection.QueryAsync<TaskModel>(write));
            }

            Assert.Equivalent(before, await connection.QueryAsync<TaskModel>(read), strict: true);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ProjectProceduresRejectAnotherUsersContextWithoutSaving(bool update)
        {
            var user = await fixture.CreateUserAsync();
            using var client = await fixture.CreateAuthenticatedClientAsync(user);
            using var otherUser = await fixture.CreateAuthenticatedClientAsync();
            var context = await CreateContextAsync(otherUser, "Other context");
            var original = update ? await CreateProjectAsync(client, "Original project") : null;
            var attempted = new ProjectModel { Id = original?.Id, UserId = user.Id, ProjectName = "Changed project", ContextId = context.Id };

            await using var connection = fixture.CreateDataConnection();
            await connection.OpenAsync(CancellationToken);
            var read = new CommandDefinition("SELECT * FROM dbo.Project WHERE UserId = @UserId",
                new { attempted.UserId }, cancellationToken: CancellationToken);
            var before = await connection.QueryAsync<ProjectModel>(read);
            var write = new CommandDefinition(update ? "dbo.spProject_UpdateForUser" : "dbo.spProject_InsertForUser",
                attempted, commandType: CommandType.StoredProcedure, cancellationToken: CancellationToken);

            if (update)
            {
                Assert.Equal(-1, await connection.QuerySingleAsync<int>(write));
            }
            else
            {
                Assert.Empty(await connection.QueryAsync<ProjectModel>(write));
            }

            Assert.Equivalent(before, await connection.QueryAsync<ProjectModel>(read), strict: true);
        }

        [Fact]
        public async Task TasksAndProjectsCanBeCreatedAndUpdatedWithSameOwnerParents()
        {
            var owner = await fixture.CreateUserAsync();
            using var client = await fixture.CreateAuthenticatedClientAsync(owner);
            var otherUser = await fixture.CreateUserAsync();
            var context = await CreateContextAsync(client, "My context");

            using var projectResponse = await HttpClientJsonExtensions.PostAsJsonAsync(client, "/api/Project/Post",
                new ProjectModel { ProjectName = "My project", ContextId = context.Id, UserId = otherUser.Id }, CancellationToken);
            var project = await ReadCreatedAsync<ProjectModel>(projectResponse);
            Assert.Equal(owner.Id, project.UserId);
            Assert.Equal(context.Id, project.ContextId);

            using var taskResponse = await HttpClientJsonExtensions.PostAsJsonAsync(client, "/api/Task/Post",
                new TaskModel { TaskName = "My task", ProjectId = project.Id, ContextId = context.Id, UserId = otherUser.Id }, CancellationToken);
            var task = await ReadCreatedAsync<TaskModel>(taskResponse);
            Assert.Equal(owner.Id, task.UserId);
            Assert.Equal(project.Id, task.ProjectId);
            Assert.Equal(context.Id, task.ContextId);

            project.ProjectName = "Updated project";
            project.UserId = otherUser.Id;
            await UpdateAsync(client, "Project", project);
            task.TaskName = "Updated task";
            task.UserId = otherUser.Id;
            await UpdateAsync(client, "Task", task);

            var savedProject = Assert.Single(await GetAsync<List<ProjectModel>>(client, Projects));
            Assert.Equal(project.ProjectName, savedProject.ProjectName);
            Assert.Equal(context.ContextName, savedProject.ContextName);
            var savedTask = Assert.Single(await GetAsync<List<TaskModel>>(client, Tasks));
            Assert.Equal(task.TaskName, savedTask.TaskName);
            Assert.Equal(project.ProjectName, savedTask.ProjectName);
            Assert.Equal(context.ContextName, savedTask.ContextName);

            using var otherClient = await fixture.CreateAuthenticatedClientAsync(otherUser);
            foreach (string route in new[] { $"/api/Task/GetAllProjectTasksById/{project.Id}", $"/api/Task/GetAllContextTasksById/{context.Id}" })
            {
                Assert.Equal(task.Id, Assert.Single(await GetAsync<List<TaskModel>>(client, route)).Id);
                Assert.Empty(await GetAsync<List<TaskModel>>(otherClient, route));
            }
        }

        [Fact]
        public async Task LegacyForeignParentsDoNotExposeNamesOrMatchCollectionFilters()
        {
            var owner = await fixture.CreateUserAsync();
            using var client = await fixture.CreateAuthenticatedClientAsync(owner);
            using var otherUser = await fixture.CreateAuthenticatedClientAsync();
            var context = await CreateContextAsync(otherUser, "Other context");
            var project = await CreateProjectAsync(otherUser, "Other project");

            await using var connection = fixture.CreateDataConnection();
            await connection.OpenAsync(CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO dbo.Task(UserId, TaskName, ProjectId, ContextId)
                VALUES (@UserId, 'Task with foreign project', @ProjectId, NULL),
                       (@UserId, 'Task with foreign context', NULL, @ContextId);

                INSERT INTO dbo.Project(UserId, ProjectName, ContextId)
                VALUES (@UserId, 'Project with foreign context', @ContextId);
                """;
            command.Parameters.AddWithValue("@UserId", owner.Id);
            command.Parameters.AddWithValue("@ProjectId", project.Id!.Value);
            command.Parameters.AddWithValue("@ContextId", context.Id!.Value);
            await command.ExecuteNonQueryAsync(CancellationToken);

            Assert.Empty(await GetAsync<List<TaskModel>>(client, $"/api/Task/GetAllProjectTasksById/{project.Id}"));
            Assert.Empty(await GetAsync<List<TaskModel>>(client, $"/api/Task/GetAllContextTasksById/{context.Id}"));

            foreach (string route in new[] { Tasks, "/api/Task/GetInboxTasksForUser" })
            {
                var tasks = await GetAsync<List<TaskModel>>(client, route);
                Assert.Equal(project.Id, Assert.Single(tasks, task => task.TaskName == "Task with foreign project").ProjectId);
                Assert.Equal(context.Id, Assert.Single(tasks, task => task.TaskName == "Task with foreign context").ContextId);
                Assert.All(tasks, task =>
                {
                    Assert.Null(task.ProjectName);
                    Assert.Null(task.ContextName);
                });
            }

            var legacyProject = Assert.Single(await GetAsync<List<ProjectModel>>(client, Projects));
            Assert.Equal(context.Id, legacyProject.ContextId);
            Assert.Null(legacyProject.ContextName);

            var legacyTask = Assert.Single(await GetAsync<List<TaskModel>>(client, Tasks), task => task.ProjectId == project.Id);
            var taskById = await GetAsync<TaskModel>(client, $"/api/Task/GetTaskById/{legacyTask.Id}");
            Assert.Equal(project.Id, taskById.ProjectId);
            Assert.Null(taskById.ProjectName);
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
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(await response.Content.ReadAsStringAsync(CancellationToken));
        }

        private static async Task DeleteAsync<T>(HttpClient client, string resource, T item)
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/{resource}/Delete")
            {
                Content = JsonContent.Create(item)
            };
            using var response = await client.SendAsync(request, CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(await response.Content.ReadAsStringAsync(CancellationToken));
        }
    }
}
