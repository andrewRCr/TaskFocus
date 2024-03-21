using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.API
{
    public class ProjectEndpoint : IProjectEndpoint
    {
        private readonly IAPIHelper _apiHelper;

        public ProjectEndpoint(IAPIHelper apiHelper)
        {
            _apiHelper = apiHelper;
        }

        public async Task<ProjectModel> GetProjectById(int projectId)
        {
            string projectIdStr = projectId.ToString();
            string requestUri = "/api/project/GetProjectById/" + projectIdStr;
            using (HttpResponseMessage response = await _apiHelper.APIClient.GetAsync(requestUri))
            {
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsAsync<ProjectModel>();
                    return result;

                }
                else { throw new Exception(response.ReasonPhrase); }
            }
        }

        public async Task<List<ProjectModel>> GetAllProjectsForUser()
        {
            using (HttpResponseMessage response = await _apiHelper.APIClient.GetAsync("/api/project/GetAllProjectsForUser"))
            {
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsAsync<List<ProjectModel>>();
                    return result;

                }
                else { throw new Exception(response.ReasonPhrase); }
            }
        }

        public async Task AddProject(ProjectModel newProject, string userId)
        {
            newProject.UserId = userId;

            using (HttpResponseMessage response = await _apiHelper.APIClient.PostAsJsonAsync("/api/project/post", newProject))
            {
                if (response.IsSuccessStatusCode)
                {
                    // TODO - log successful insert call ?
                }
                else { throw new Exception(response.ReasonPhrase); }
            }
        }

        public async Task UpdateProject(ProjectModel updatedProject)
        {
            using (HttpResponseMessage response = await _apiHelper.APIClient.PutAsJsonAsync("/api/project/put", updatedProject))
            {
                if (response.IsSuccessStatusCode)
                {
                    // TODO - log successful update call ?
                }
                else { throw new Exception(response.ReasonPhrase); }
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

            using (HttpResponseMessage response = await _apiHelper.APIClient.SendAsync(request))
            {
                if (response.IsSuccessStatusCode)
                {
                    // TODO - log successful delete call ?
                }
                else { throw new Exception(response.ReasonPhrase); }
            }
        }
    }
}
