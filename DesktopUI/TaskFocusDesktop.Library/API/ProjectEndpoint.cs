using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TaskFocusDesktop.Library.Models;

namespace TaskFocusDesktop.Library.API
{
    public class ProjectEndpoint : IProjectEndpoint
    {
        private IAPIHelper _apiHelper;

        public ProjectEndpoint(IAPIHelper apiHelper)
        {
            _apiHelper = apiHelper;
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

        public Task DeleteProject(ProjectModel projectToDelete)
        {
            throw new NotImplementedException();
        }

        public Task UpdateProject(ProjectModel projectToUpdate)
        {
            throw new NotImplementedException();
        }
    }
}
