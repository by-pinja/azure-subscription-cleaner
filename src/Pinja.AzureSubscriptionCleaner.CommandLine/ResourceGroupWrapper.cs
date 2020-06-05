using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Extensions.Logging;

namespace Pinja.AzureSubscriptionCleaner.CommandLine
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

        public async Task<IReadOnlyList<string>> DeleteNonLockedResourceGroups(bool simulate)
        {
            _logger.LogTrace("Finding resource groups to delete...");
            var resourceGroups = await _azureConnection.ResourceGroups.ListAsync(true).ConfigureAwait(false);
            _logger.LogDebug("Found {count} resource groups to delete.", resourceGroups.Count());
            var deletedGroupNames = new List<string>();
            foreach (var resourceGroup in resourceGroups)
            {
                if (await DeleteNonLockedResourceGroup(resourceGroup, simulate).ConfigureAwait(false))
                {
                    deletedGroupNames.Add(resourceGroup.Name);
                }
            }
            _logger.LogTrace("Resource groups deleted");
            return deletedGroupNames;
        }

        private async Task<bool> DeleteNonLockedResourceGroup(IResourceGroup resourceGroup, bool simulate)
        {
            _logger.LogDebug("Checking if resource group {resourceGroup} has locks...", resourceGroup.Name);
            var locks = await _azureConnection.ManagementLocks.ListByResourceGroupAsync(resourceGroup.Name, true).ConfigureAwait(false);

            if (locks.Any())
            {
                foreach (var managementLock in locks)
                {
                    _logger.LogDebug("Lock found in resource group {resourceGroup}, id: {id}, level: {level} notes: {notes}", resourceGroup.Name, managementLock.Id, managementLock.Level, managementLock.Notes);
                }
                _logger.LogInformation("Resource group {resourceGroup} had at least one lock, skipping deletion.", resourceGroup.Name);
                return false;
            }
            _logger.LogInformation("Deleting resource group {resourceGroup}", resourceGroup.Name);
            if (!simulate)
            {
                await _azureConnection.ResourceGroups.DeleteByNameAsync(resourceGroup.Name).ConfigureAwait(false);
            }
            return true;
        }
    }
}
