using System.Collections.Generic;

namespace Pinja.AzureSubscriptionCleaner.AzureFunctions
{
    public class SlackReportingContext
    {
        public List<string> DeletedResourceGroups { get; set; }
    }
}
