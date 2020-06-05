using Newtonsoft.Json;

namespace Protacon.AzureSubscriptionCleaner.SlackLib.Dto
{
    public class TextDto
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }
}