using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
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
            if (builder is null)
            {
                throw new System.ArgumentNullException(nameof(builder));
            }

            builder
                .Services
                .AddTransient((provider) =>
                {
                    return BuildConnection();
                })
                .AddLogging();
        }

        private static IAzure BuildConnection()
        {
            var msiInformation = new MSILoginInformation(MSIResourceType.AppService);

            var credentials = SdkContext.AzureCredentialsFactory.FromMSI(msiInformation, AzureEnvironment.AzureGlobalCloud);
            return Azure
                    .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(credentials)
                    .WithDefaultSubscription();
        }
    }
}