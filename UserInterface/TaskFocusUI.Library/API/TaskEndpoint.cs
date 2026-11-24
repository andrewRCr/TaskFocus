using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.API
{
    public class TaskEndpoint : ITaskEndpoint
    {
        private readonly IAPIHelper _apiHelper;
        private readonly ILogger<TaskEndpoint> _logger;

        public TaskEndpoint(IAPIHelper apiHelper, ILogger<TaskEndpoint> logger = null)
        {
            _apiHelper = apiHelper;
            _logger = logger;
        }

        public async Task<TaskModel> GetTaskById(int taskId)
        {
            string taskIdStr = taskId.ToString();
            string requestUri = "/api/task/GetTaskById/" + taskIdStr;
            using (HttpResponseMessage response = await ApiRequest.SendAsync(_apiHelper.APIClient.GetAsync(requestUri)))
            {
                var result = await response.Content.ReadAsAsync<TaskModel>();
                return result;
            }
        }

        public async Task<List<TaskModel>> GetAllTasksForUser()
        {
            using (HttpResponseMessage response = await ApiRequest.SendAsync(_apiHelper.APIClient.GetAsync("/api/task/GetAllTasksForUser")))
            {
                var result = await response.Content.ReadAsAsync<List<TaskModel>>();
                return result;
            }
        }

        public async Task<List<TaskModel>> GetInboxTasksForUser()
        {
            using (HttpResponseMessage response = await ApiRequest.SendAsync(_apiHelper.APIClient.GetAsync("/api/task/GetInboxTasksForUser")))
            {
                var result = await response.Content.ReadAsAsync<List<TaskModel>>();
                return result;
            }
        }

        public async Task<List<TaskModel>> GetAllProjectTasksById(int projectId)
        {
            string projectIdStr = projectId.ToString();
            string requestUri = "/api/task/GetAllProjectTasksById/" + projectIdStr;
            using (HttpResponseMessage response = await ApiRequest.SendAsync(_apiHelper.APIClient.GetAsync(requestUri)))
            {
                var result = await response.Content.ReadAsAsync<List<TaskModel>>();
                return result;
            }
        }

        public async Task<List<TaskModel>> GetAllContextTasksById(int contextId)
        {
            string contextIdStr = contextId.ToString();
            string requestUri = "/api/task/GetAllContextTasksById/" + contextIdStr;
            using (HttpResponseMessage response = await ApiRequest.SendAsync(_apiHelper.APIClient.GetAsync(requestUri)))
            {
                var result = await response.Content.ReadAsAsync<List<TaskModel>>();
                return result;
            }
        }

        public async Task<TaskModel> AddTask(TaskModel task, string userId)
        {
            task.UserId = userId;

            using (HttpResponseMessage response = await ApiRequest.SendAsync(_apiHelper.APIClient.PostAsJsonAsync("/api/task/post", task)))
            {
                _logger?.LogInformation("API: AddTask request processed successfully.");
                var result = await response.Content.ReadAsAsync<TaskModel>();
                return result;
            }
        }

        public async Task UpdateTask(TaskModel task)
        {
            using (HttpResponseMessage response = await ApiRequest.SendAsync(_apiHelper.APIClient.PutAsJsonAsync("/api/task/put", task)))
            {
                _logger?.LogInformation("API: UpdateTask request processed successfully.");
            }
        }

        public async Task DeleteTask(TaskModel task)
        {
            var p = new { Id = task.Id };
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri("/api/task/delete", UriKind.Relative),
                Content = new StringContent(JsonConvert.SerializeObject(p), Encoding.UTF8, "application/json")
            };

            using (HttpResponseMessage response = await ApiRequest.SendAsync(_apiHelper.APIClient.SendAsync(request)))
            {
                _logger?.LogInformation("API: DeleteTask request processed successfully.");
            }
        }
    }
}
