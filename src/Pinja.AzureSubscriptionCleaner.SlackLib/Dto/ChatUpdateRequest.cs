using Newtonsoft.Json;

namespace Pinja.AzureSubscriptionCleaner.SlackLib.Dto
{
    public class ChatUpdateRequest
    {
        [JsonProperty("ts")]
        public string TimeStamp { get; set; }

        [JsonProperty("channel")]
        public string Channel { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("blocks")]
        public Section[] Blocks { get; set; }
    }
}