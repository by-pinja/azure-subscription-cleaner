using System;
using System.Collections.Generic;

#pragma warning disable CA2227 // This is a DTO
namespace Pinja.AzureSubscriptionCleaner.AzureFunctions
{
    public class SlackReportingContext
    {
        public List<string> DeletedResourceGroups { get; set; }
    }
}
