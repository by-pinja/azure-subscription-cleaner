namespace Protacon.AzureSubscriptionCleaner.SlackLib
{
    /// <summary>
    /// Thrown when something goes wrong with Slack API
    /// </summary>
    [System.Serializable]
    public class SlackLibException : System.Exception
    {
        public SlackLibException() { }
        public SlackLibException(string message) : base(message) { }
        public SlackLibException(string message, System.Exception inner) : base(message, inner) { }
        protected SlackLibException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
