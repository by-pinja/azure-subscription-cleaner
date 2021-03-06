using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pinja.AzureSubscriptionCleaner.AzureFunctions;
using Pinja.AzureSubscriptionCleaner.SlackLib;


[assembly: FunctionsStartup(typeof(Startup))]
namespace Pinja.AzureSubscriptionCleaner.AzureFunctions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            IConfiguration config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            builder
                .Services
                .AddHttpClient<ISlackClient, SlackClient>((provider, client) =>
                {
                    var slackClientSettings = provider.GetService<SlackClientSettings>();
                    client.BaseAddress = new Uri("https://slack.com/api/");
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {slackClientSettings.BearerToken}");
                });

            builder
                .Services
                .AddTransient(provider => BuildConnection())
                .AddTransient(prodvider => config.GetSection("CleanupConfiguration").Get<CleanupConfiguration>())
                .AddTransient(prodvider => config.GetSection("SlackClientSettings").Get<SlackClientSettings>())
                .AddLogging();
        }

        private static IAzure BuildConnection()
        {
            var msiInformation = new MSILoginInformation(MSIResourceType.AppService);

            var credentials = SdkContext.AzureCredentialsFactory.FromMSI(msiInformation, AzureEnvironment.AzureGlobalCloud);
            return Microsoft.Azure.Management.Fluent.Azure
                    .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(credentials)
                    .WithDefaultSubscription();
        }
    }
}