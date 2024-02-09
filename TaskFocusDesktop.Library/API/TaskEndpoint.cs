using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TaskFocusDesktop.Library.Models;

namespace TaskFocusDesktop.Library.API
{
    public class TaskEndpoint : ITaskEndpoint
    {
        private IAPIHelper _apiHelper;

        public TaskEndpoint(IAPIHelper apiHelper)
        {
            _apiHelper = apiHelper;
        }

        public async Task<List<TaskModel>> GetAllForUser()
        {
            using (HttpResponseMessage response = await _apiHelper.APIClient.GetAsync("/api/task/GetAll"))
            {
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsAsync<List<TaskModel>>();
                    return result;

                }
                else { throw new Exception(response.ReasonPhrase); }
            }
        }

        public async Task<List<TaskModel>> GetInboxTasks()
        {
            using (HttpResponseMessage response = await _apiHelper.APIClient.GetAsync("/api/task/GetInbox"))
            {
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsAsync<List<TaskModel>>();
                    return result;

                }
                else { throw new Exception(response.ReasonPhrase); }
            }
        }

        public async Task AddTask(TaskModel task, string userId)
        {
            task.UserId = userId;

            using (HttpResponseMessage response = await _apiHelper.APIClient.PostAsJsonAsync("/api/task", task))
            {
                if (response.IsSuccessStatusCode)
                {
                    // TODO - log successful update call ?
                }
                else { throw new Exception(response.ReasonPhrase); }
            }
        }

        public async Task UpdateTask(TaskModel task)
        {
            using (HttpResponseMessage response = await _apiHelper.APIClient.PutAsJsonAsync("/api/task", task))
            {
                if (response.IsSuccessStatusCode)
                {
                    // TODO - log successful update call ?
                }
                else { throw new Exception(response.ReasonPhrase); }
            }
        }
    }
}
