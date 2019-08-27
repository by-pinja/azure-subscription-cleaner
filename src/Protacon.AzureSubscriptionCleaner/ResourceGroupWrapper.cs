﻿using System;
using System.Linq;
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

        public async Task DeleteNonLockedResourceGroups(bool simulate)
        {
            _logger.LogTrace("Finding resource groups to delete...");
            var resourceGroups = await _azureConnection.ResourceGroups.ListAsync(true);
            _logger.LogDebug("Found {count} resource groups to delete.", resourceGroups.Count());
            foreach (var resourceGroup in resourceGroups)
            {
                _logger.LogInformation("Deleting {resourceGroupName}", resourceGroup.Name);
                if (!simulate)
                {
                    await _azureConnection.ResourceGroups.DeleteByNameAsync(resourceGroup.Name);
                }
            }
            _logger.LogTrace("Resource groups deleted");
        }
    }
}
