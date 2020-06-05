namespace Protacon.AzureSubscriptionCleaner.AzureFunctions
{
    /// <summary>
    /// This is used to automatically map reporting configuration from IConfiguration
    /// </summary>
    public class ReportingConfiguration
    {
        public string SlackChannel { get; set; }
    }
}