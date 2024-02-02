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

        public async Task<List<ProjectModel>> GetAllForUser()
        {
            using (HttpResponseMessage response = await _apiHelper.APIClient.GetAsync("/api/project"))
            {
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsAsync<List<ProjectModel>>();
                    return result;

                }
                else { throw new Exception(response.ReasonPhrase); }
            }
        }
    }
}
