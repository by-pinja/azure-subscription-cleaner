using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Protacon.AzureSubscriptionCleaner.CommandLine
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using var dependencyInjection = BuildDependencyInjection();
            var logger = dependencyInjection.GetService<ILogger<Program>>();
            Parser
                .Default
                .ParseArguments<Options>(args)
                .MapResult(
                    async (Options option) => { await ProcessOptions(option, dependencyInjection); },
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
        }

        private static async Task ProcessOptions(Options options, ServiceProvider serviceProvider)
        {
            var resourceGroupWrapper = serviceProvider.GetService<ResourceGroupWrapper>();
            await resourceGroupWrapper.DeleteNonLockedResourceGroups(options.Simulate);
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

                    return AzureConnectionBuilder.BuildServicePrincipalConnection(servicePrincipalConfiguration);
                })
                .AddTransient<ResourceGroupWrapper>()
                .BuildServiceProvider();
        }
    }
}
