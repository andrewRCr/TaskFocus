using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.API
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

        public async Task AddContext(ContextModel newContext, string userId)
        {
            newContext.UserId = userId;

            using (HttpResponseMessage response = await _apiHelper.APIClient.PostAsJsonAsync("/api/context/post", newContext))
            {
                if (response.IsSuccessStatusCode)
                {
                    // TODO - log successful insert call ?
                }
                else { throw new Exception(response.ReasonPhrase); }
            }
        }

        public Task DeleteContext(ContextModel contextToDelete)
        {
            throw new NotImplementedException();
        }

        public Task UpdateContext(ContextModel contextToUpdate)
        {
            throw new NotImplementedException();
        }
    }
}
