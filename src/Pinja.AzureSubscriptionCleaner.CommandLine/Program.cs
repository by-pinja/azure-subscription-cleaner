using System;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pinja.AzureSubscriptionCleaner.SlackLib;
using static Pinja.AzureSubscriptionCleaner.SlackLib.MessageUtil;

namespace Pinja.AzureSubscriptionCleaner.CommandLine
{
#pragma warning disable CA1052
    public class Program
#pragma warning restore CA1052
    {
        public static void Main(string[] args)
        {
            using var dependencyInjection = BuildDependencyInjection();
            var logger = dependencyInjection.GetRequiredService<ILogger<Program>>();
            var start = DateTime.UtcNow;
            logger.LogDebug("Starting time: {Time}", start);

            Parser
                .Default
                .ParseArguments<ProgramOptions>(args)
                .MapResult(
                    async (ProgramOptions option) => { await ProcessOptions(option, dependencyInjection).ConfigureAwait(false); },
                    errors =>
                    {
                        if (errors.Count() == 1 &&
                            (
                                errors.First().Tag == ErrorType.HelpVerbRequestedError ||
                                errors.First().Tag == ErrorType.HelpRequestedError ||
                                errors.First().Tag == ErrorType.VersionRequestedError ||
                                errors.First().Tag == ErrorType.NoVerbSelectedError))
                        {
                            return Task.CompletedTask;
                        }

                        logger.LogWarning("Something went wrong while parsing command(s): Errors: {Errors}", string.Join(", ", errors));
                        return Task.CompletedTask;
                    }).Wait();

            logger.LogDebug("Run duration: {Duration}", DateTime.UtcNow - start);
        }

        private static async Task ProcessOptions(ProgramOptions options, ServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            var resourceGroupWrapper = serviceProvider.GetService<ResourceGroupWrapper>();
            if (resourceGroupWrapper == null)
            {
                logger.LogError("Unable to create {Name}. Aborting.", nameof(ResourceGroupWrapper));
                return;
            }

            var deletedResourceGroups = await resourceGroupWrapper.DeleteNonLockedResourceGroups(options.Simulate).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(options.Channel))
            {
                var slackClient = serviceProvider.GetRequiredService<ISlackClient>();
                if (slackClient == null)
                {
                    logger.LogError("Unable to create {Name}. Aborting.", nameof(ISlackClient));
                    return;
                }
                var context = new MessageContext
                {
                    DeletedResourceGroups = deletedResourceGroups,
                    WasSimulated = options.Simulate
                };

                var message = MessageUtil.CreateDeleteInformationMessage(options.Channel, context);
                await slackClient.PostMessage(message).ConfigureAwait(false);
            }
        }

        private static ServiceProvider BuildDependencyInjection()
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            return new ServiceCollection()
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.AddConfiguration(config.GetSection("Logging"));
                    loggingBuilder.AddConsole();
                })
                .AddTransient((provider) =>
                {
                    var servicePrincipalConfigSection = config.GetSection("ServicePrincipalConfiguration");
                    var servicePrincipalConfiguration = new ServicePrincipalConfiguration(
                        servicePrincipalConfigSection["TenantId"],
                        servicePrincipalConfigSection["ClientId"],
                        servicePrincipalConfigSection["ClientSecret"]
                    );

                    return BuildServicePrincipalConnection(servicePrincipalConfiguration);
                })
                .AddTransient(prodvider =>
                {
                    return config.GetSection("SlackClientSettings").Get<SlackClientSettings>();
                })
                .AddTransient<ResourceGroupWrapper>()
                .AddHttpClient<ISlackClient, SlackClient>((provider, client) =>
                {
                    var slackClientSettings = provider.GetService<SlackClientSettings>();
                    if (slackClientSettings == null || string.IsNullOrWhiteSpace(slackClientSettings.BearerToken))
                    {
                        throw new ArgumentException("SlackClientSettings must be defined for Slack communication.");
                    }

                    client.BaseAddress = new Uri("https://slack.com/api/");
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {slackClientSettings.BearerToken}");
                })
                .Services
                .BuildServiceProvider();
        }

        private static IAzure BuildServicePrincipalConnection(ServicePrincipalConfiguration servicePrincipal)
        {
            var credentials = SdkContext
                .AzureCredentialsFactory
                .FromServicePrincipal(servicePrincipal.ClientId, servicePrincipal.ClientSecret, servicePrincipal.TenantId, AzureEnvironment.AzureGlobalCloud);
            return Azure
                .Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(credentials)
                .WithDefaultSubscription();
        }
    }
}
