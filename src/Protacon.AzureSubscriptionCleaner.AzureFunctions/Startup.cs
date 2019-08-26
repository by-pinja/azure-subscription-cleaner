using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Protacon.AzureSubscriptionCleaner.AzureFunctions;

[assembly: WebJobsStartup(typeof(Startup))]
namespace Protacon.AzureSubscriptionCleaner.AzureFunctions
{
    public class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder
                .Services
                .AddLogging();
        }
    }
}