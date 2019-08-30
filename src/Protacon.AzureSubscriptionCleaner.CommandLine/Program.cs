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

namespace Protacon.AzureSubscriptionCleaner.CommandLine
{
#pragma warning disable CA1052
    public class Program
#pragma warning restore CA1052
    {
        public static void Main(string[] args)
        {
            using var dependencyInjection = BuildDependencyInjection();
            var logger = dependencyInjection.GetService<ILogger<Program>>();
            var start = DateTime.UtcNow;
            logger.LogDebug("Starting time: {time}", start);
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
                            return Task.FromResult(0);
                        }

                        logger.LogWarning("Something went wrong while parsing command(s): Errors: {errors}", string.Join(", ", errors));
                        return Task.FromResult(0);
                    }).Wait();
            logger.LogDebug("Run duration: {duration}", DateTime.UtcNow - start);
        }

        private static async Task ProcessOptions(ProgramOptions options, ServiceProvider serviceProvider)
        {

            var resourceGroupWrapper = serviceProvider.GetService<ResourceGroupWrapper>();
            await resourceGroupWrapper.DeleteNonLockedResourceGroups(options.Simulate).ConfigureAwait(false);
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
                .AddTransient<ResourceGroupWrapper>()
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
