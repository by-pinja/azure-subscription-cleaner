#pragma warning disable CA1819 // This is a DTO
using Newtonsoft.Json;

namespace Protacon.AzureSubscriptionCleaner.SlackLib.Dto
{
    public class PostMessage
    {
        [JsonProperty("channel")]
        public string Channel { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("blocks")]
        public Section[] Blocks { get; set; }
    }
}
#pragma warning restore CA1819