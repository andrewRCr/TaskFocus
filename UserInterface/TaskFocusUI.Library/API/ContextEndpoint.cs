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
    public class ContextEndpoint : IContextEndpoint
    {
        private readonly IAPIHelper _apiHelper;
        private readonly ILogger<ContextEndpoint> _logger;

        public ContextEndpoint(IAPIHelper apiHelper, ILogger<ContextEndpoint> logger = null)
        {
            _apiHelper = apiHelper;
            _logger = logger;
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
                    _logger?.LogInformation("API: AddContext request processed successfully.");
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
                    _logger?.LogInformation("API: UpdateContext request processed successfully.");
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
                    _logger?.LogInformation("API: DeleteContext request processed successfully.");
                }
                else { throw new Exception(response.ReasonPhrase); }
            }
        }
    }
}
