using Newtonsoft.Json;
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
        private readonly IAPIHelper _apiHelper;

        public ContextEndpoint(IAPIHelper apiHelper)
        {
            _apiHelper = apiHelper;
        }

        public async Task<ContextModel> GetContextById(int contextId)
        {
            string contextIdStr = contextId.ToString();
            string requestUri = "/api/context/GetContextById/" + contextIdStr;
            using (HttpResponseMessage response = await _apiHelper.APIClient.GetAsync(requestUri))
            {
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsAsync<ContextModel>();
                    return result;

                }
                else { throw new Exception(response.ReasonPhrase); }
            }
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

        public async Task UpdateContext(ContextModel updatedContext)
        {
            using (HttpResponseMessage response = await _apiHelper.APIClient.PutAsJsonAsync("/api/context/put", updatedContext))
            {
                if (response.IsSuccessStatusCode)
                {
                    // TODO - log successful update call ?
                }
                else { throw new Exception(response.ReasonPhrase); }
            }
        }

        public async Task DeleteContext(ContextModel contextToDelete)
        {
            var p = new { Id = contextToDelete.Id };
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri("/api/context/delete", UriKind.Relative),
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
