using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TaskFocusDesktop.Library.Models;

namespace TaskFocusDesktop.Library.API
{
    public class ContextEndpoint : IContextEndpoint
    {
        private IAPIHelper _apiHelper;

        public ContextEndpoint(IAPIHelper apiHelper)
        {
            _apiHelper = apiHelper;
        }

        public async Task<List<ContextModel>> GetAllContextsForUser()
        {
            using (HttpResponseMessage response = await _apiHelper.APIClient.GetAsync("/api/context/GetAllContextsForUser"))
            {
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsAsync<List<ContextModel>>();
                    return result;

                }
                else { throw new Exception(response.ReasonPhrase); }
            }
        }
    }
}
