using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.API
{
    public class ProjectEndpoint : IProjectEndpoint
    {
        private readonly IAPIHelper _apiHelper;
        private readonly ILogger<ProjectEndpoint> _logger;

        public ProjectEndpoint(IAPIHelper apiHelper, ILogger<ProjectEndpoint> logger = null)
        {
            _apiHelper = apiHelper;
            _logger = logger;
        }

        public async Task<ProjectModel> GetProjectById(int projectId)
        {
            string projectIdStr = projectId.ToString();
            string requestUri = "/api/project/GetProjectById/" + projectIdStr;
            using (HttpResponseMessage response = await ApiRequest.SendAsync(_apiHelper.APIClient.GetAsync(requestUri)))
            {
                var result = await response.Content.ReadAsAsync<ProjectModel>();
                return result;
            }
        }

        public async Task<List<ProjectModel>> GetAllProjectsForUser()
        {
            using (HttpResponseMessage response = await ApiRequest.SendAsync(_apiHelper.APIClient.GetAsync("/api/project/GetAllProjectsForUser")))
            {
                var result = await response.Content.ReadAsAsync<List<ProjectModel>>();
                return result;
            }
        }

        public async Task<ProjectModel> AddProject(ProjectModel newProject, string userId)
        {
            newProject.UserId = userId;

            using (HttpResponseMessage response = await ApiRequest.SendAsync(_apiHelper.APIClient.PostAsJsonAsync("/api/project/post", newProject)))
            {
                _logger?.LogInformation("API: AddProject request processed successfully.");
                var result = await response.Content.ReadAsAsync<ProjectModel>();
                return result;
            }
        }

        public async Task UpdateProject(ProjectModel updatedProject)
        {
            using (HttpResponseMessage response = await ApiRequest.SendAsync(_apiHelper.APIClient.PutAsJsonAsync("/api/project/put", updatedProject)))
            {
                _logger?.LogInformation("API: UpdateProject request processed successfully.");
            }
        }

        public async Task DeleteProject(ProjectModel projectToDelete)
        {
            var p = new { Id = projectToDelete.Id };
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri("/api/project/delete", UriKind.Relative),
                Content = new StringContent(JsonConvert.SerializeObject(p), Encoding.UTF8, "application/json")
            };

            using (HttpResponseMessage response = await ApiRequest.SendAsync(_apiHelper.APIClient.SendAsync(request)))
            {
                _logger?.LogInformation("API: DeleteProject request processed successfully.");
            }
        }
    }
}
