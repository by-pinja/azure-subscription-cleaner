namespace Pinja.AzureSubscriptionCleaner.AzureFunctions
{
    /// <summary>
    /// This is used to automatically map configurations used in Azure Functions App
    /// </summary>
    public class CleanupConfiguration
    {
        /// <summary>
        /// This is the target channel where slack report is sent.
        /// </summary>
        /// <value></value>
        public string SlackChannel { get; set; }

        /// <summary>
        /// If true, nothing is deleted, only simulation is done!
        /// </summary>
        /// <value></value>
        public bool Simulate { get; set; }
    }
}