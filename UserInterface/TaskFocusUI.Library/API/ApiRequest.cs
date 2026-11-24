using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace TaskFocusUI.Library.API
{
    public static class ApiRequest
    {
        public const string ServiceUnavailableMessage = "TaskFocus is starting up or being updated. Please try again in a minute.";
        public const string TooManyRequestsMessage = "There have been too many attempts. Please wait and try again.";
        public const string RequestFailedMessage = "Could not complete the request. Please try again.";

        public static bool IsServiceFailure(HttpRequestException error) =>
            error.StatusCode is null || IsServiceFailureStatus(error.StatusCode.Value);

        public static Task<HttpResponseMessage> SendAsync(Task<HttpResponseMessage> request) =>
            SendAsync(request, accountRequest: false);

        internal static Task<HttpResponseMessage> SendAccountAsync(Task<HttpResponseMessage> request) =>
            SendAsync(request, accountRequest: true);

        private static async Task<HttpResponseMessage> SendAsync(Task<HttpResponseMessage> request, bool accountRequest)
        {
            HttpResponseMessage response;
            try
            {
                response = await request;
            }
            catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
            {
                var message = accountRequest
                    ? "Could not connect to the account service. Check your connection and try again."
                    : ServiceUnavailableMessage;
                throw new HttpRequestException(message, ex);
            }

            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            using (response)
            {
                var status = response.StatusCode;
                var message = accountRequest
                    ? await GetAccountFailureMessage(response)
                    : GetFailureMessage(status);

                throw new HttpRequestException(message, inner: null, status);
            }
        }

        private static bool IsServiceFailureStatus(HttpStatusCode status) =>
            status == HttpStatusCode.RequestTimeout || (int)status is >= 500 and < 600;

        private static string GetFailureMessage(HttpStatusCode status) => status switch
        {
            HttpStatusCode.TooManyRequests => TooManyRequestsMessage,
            _ when IsServiceFailureStatus(status) => ServiceUnavailableMessage,
            _ => RequestFailedMessage
        };

        private static async Task<string> GetAccountFailureMessage(HttpResponseMessage response)
        {
            var status = response.StatusCode;
            bool rejected = (int)status is >= 400 and < 500;

            // Server failures may contain diagnostics. Only read user-facing rejection details.
            var message = rejected ? await ReadRejectionMessage(response.Content) : null;
            return message ?? (status switch
            {
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                    "This account request is not allowed. Please sign in to continue.",
                HttpStatusCode.NotFound or HttpStatusCode.Gone =>
                    "This account link is invalid or has expired. Please request a new link.",
                HttpStatusCode.TooManyRequests => TooManyRequestsMessage,
                _ when rejected => "The account request was rejected. Check your details and try again.",
                _ => "The account service is unavailable. Please try again later."
            });
        }

        private static async Task<string?> ReadRejectionMessage(HttpContent content)
        {
            try
            {
                using var body = await JsonDocument.ParseAsync(await content.ReadAsStreamAsync());
                var root = body.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                {
                    return null;
                }

                var errors = root;
                foreach (var property in root.EnumerateObject())
                {
                    if (property.Name.Equals("message", StringComparison.OrdinalIgnoreCase) &&
                        property.Value.ValueKind == JsonValueKind.String &&
                        property.Value.GetString() is string message && !string.IsNullOrWhiteSpace(message))
                    {
                        return message;
                    }

                    if (property.NameEquals("errors") && property.Value.ValueKind == JsonValueKind.Object)
                    {
                        errors = property.Value;
                    }
                }

                foreach (var field in errors.EnumerateObject())
                {
                    if (field.Value.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    var messages = field.Value.EnumerateArray()
                        .Where(error => error.ValueKind == JsonValueKind.String)
                        .Select(error => error.GetString())
                        .Where(message => !string.IsNullOrWhiteSpace(message));
                    var text = string.Join(" ", messages);
                    if (text.Length > 0)
                    {
                        return text;
                    }
                }
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException or System.IO.IOException or HttpRequestException)
            {
                // Empty, non-JSON, invalid-text and interrupted responses use the fallback for their status.
            }

            return null;
        }
    }
}
