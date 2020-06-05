using Newtonsoft.Json;

namespace Pinja.AzureSubscriptionCleaner.SlackLib.Dto
{
    public class Section
    {
        [JsonProperty("type")]
        public string Type { get; } = "section";

        [JsonProperty("text")]
        public TextDto Text { get; set; }
    }
}