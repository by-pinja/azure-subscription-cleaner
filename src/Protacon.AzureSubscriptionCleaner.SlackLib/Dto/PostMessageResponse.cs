using Newtonsoft.Json;

namespace Protacon.AzureSubscriptionCleaner.SlackLib.Dto
{
    public class PostMessageResponse : SlackResponse
    {
        /// <summary>
        /// NOTE: Channel is in "ID" format here
        /// </summary>
        /// <value></value>
        [JsonProperty("channel")]
        public string Channel { get; set; }

        [JsonProperty("ts")]
        public string TimeStamp { get; set; }
    }
}