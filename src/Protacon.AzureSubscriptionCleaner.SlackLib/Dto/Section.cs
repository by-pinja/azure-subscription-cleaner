using Newtonsoft.Json;

namespace Protacon.AzureSubscriptionCleaner.SlackLib.Dto
{
    public class Section
    {
        [JsonProperty("type")]
        public string Type { get; } = "section";

        [JsonProperty("text")]
        public TextDto Text { get; set; }
    }
}