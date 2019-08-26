using System;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Extensions.Logging;

namespace Protacon.AzureSubscriptionCleaner
{
    public class ResourceGroupWrapper
    {
        private readonly ILogger<ResourceGroupWrapper> _logger;
        private readonly IAzure _azureConnection;

        public ResourceGroupWrapper(ILogger<ResourceGroupWrapper> logger, IAzure azureConnection)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _azureConnection = azureConnection ?? throw new ArgumentNullException(nameof(azureConnection));
        }

        public async Task ListGroups()
        {
            _logger.LogTrace("Finding resource groups...");
            var resourceGroups = await _azureConnection.ResourceGroups.ListAsync(true);
            foreach (var resourceGroup in resourceGroups)
            {
                _logger.LogDebug("Found resource group {resourceGroupName}", resourceGroup.Name);
                //await _azureConnection.ResourceGroups.DeleteByNameAsync(resourceGroup.Name);
            }
        }
    }
}
