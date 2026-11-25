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
            using (HttpResponseMessage response = await ApiRequest.SendAsync(_apiHelper.APIClient.GetAsync(requestUri)))
            {
                var result = await response.Content.ReadAsAsync<ContextModel>();
                return result;
            }
        }

        public async Task<List<ContextModel>> GetAllContextsForUser()
        {
            using (HttpResponseMessage response = await ApiRequest.SendAsync(_apiHelper.APIClient.GetAsync("/api/context/GetAllContextsForUser")))
            {
                var result = await response.Content.ReadAsAsync<List<ContextModel>>();
                return result;
            }
        }

        public async Task<ContextModel> AddContext(ContextModel newContext, string userId)
        {
            newContext.UserId = userId;

            using (HttpResponseMessage response = await ApiRequest.SendAsync(_apiHelper.APIClient.PostAsJsonAsync("/api/context/PostWithResult", newContext)))
            {
                _logger?.LogInformation("API: AddContext request processed successfully.");
                var result = await response.Content.ReadAsAsync<ContextModel>();
                return result;
            }
        }

        public async Task UpdateContext(ContextModel updatedContext)
        {
            using (HttpResponseMessage response = await ApiRequest.SendAsync(_apiHelper.APIClient.PutAsJsonAsync("/api/context/put", updatedContext)))
            {
                _logger?.LogInformation("API: UpdateContext request processed successfully.");
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

            using (HttpResponseMessage response = await ApiRequest.SendAsync(_apiHelper.APIClient.SendAsync(request)))
            {
                _logger?.LogInformation("API: DeleteContext request processed successfully.");
            }
        }
    }
}
