using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Pinja.AzureSubscriptionCleaner.SlackLib.Dto;

namespace Pinja.AzureSubscriptionCleaner.SlackLib
{
    /// <summary>
    /// Wrapper for Slack API
    /// </summary>
    public class SlackClient : ISlackClient
    {
        private const string ContentType = "application/json";

        private readonly ILogger<SlackClient> _logger;
        private readonly HttpClient _client;

        public SlackClient(ILogger<SlackClient> logger, HttpClient client)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<PostMessageResponse> PostMessage(PostMessage message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var serializedPayload = JsonConvert.SerializeObject(message);
            using var postContent = new StringContent(serializedPayload, Encoding.UTF8, ContentType);
            _logger.LogTrace("Json: {rawJsonContent}", serializedPayload);

            using var response = await _client.PostAsync("chat.postMessage", postContent).ConfigureAwait(false);
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            var parsed = JsonConvert.DeserializeObject<PostMessageResponse>(content);
            if (response.StatusCode != System.Net.HttpStatusCode.OK || !parsed.Ok)
            {
                _logger.LogError("Status code: {statusCode}. Error message: {error}", response.StatusCode, parsed?.Error);
                _logger.LogError("Content: {content}", content);
                throw new SlackLibException($"Something went wrong while posting alert. Code was {response.StatusCode}, message: {parsed?.Error}");
            }
            return parsed;
        }
    }
}
