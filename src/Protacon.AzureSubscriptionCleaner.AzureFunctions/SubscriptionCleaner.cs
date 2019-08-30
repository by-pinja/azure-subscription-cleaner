using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Protacon.AzureSubscriptionCleaner.AzureFunctions
{
    public class SubscriptionCleaner
    {
        private readonly ILogger<SubscriptionCleaner> _logger;
        private readonly IAzure _azureConnection;

        public SubscriptionCleaner(ILogger<SubscriptionCleaner> logger, IAzure azureConnection)
        {
            _logger = logger;
            _azureConnection = azureConnection;
        }

        [FunctionName(nameof(TimerStart))]
        public async Task TimerStart([TimerTrigger("0 2 1 * * *")] TimerInfo timer, [OrchestrationClient] IDurableOrchestrationClient starter)
        {
            _logger.LogTrace("{class}, Next: {next}, Last: {last}", nameof(TimerStart), timer.ScheduleStatus.Next, timer.ScheduleStatus.Last);
            string instanceId = await starter.StartNewAsync(nameof(OchestrateSubscriptionCleanUp), null);
            _logger.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        }

        [FunctionName(nameof(OchestrateSubscriptionCleanUp))]
        public async Task OchestrateSubscriptionCleanUp([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            if (!context.IsReplaying)
            {
                _logger.LogDebug("Getting resource groups");
            }
            var resourceGroupNames = await context.CallActivityAsync<IEnumerable<string>>(nameof(GetResourceGroupsNames), null);

            foreach (var name in resourceGroupNames)
            {
                if (!context.IsReplaying)
                {
                    _logger.LogDebug("Calling delete function on {resourceGroupName}", name);
                }
                await context.CallActivityAsync(nameof(DeleteIfNotLocked), name);
            }
        }

        [FunctionName(nameof(GetResourceGroupsNames))]
        public async Task<IEnumerable<string>> GetResourceGroupsNames([ActivityTrigger] IDurableActivityContext context)
        {
            _logger.LogTrace("Instance {instanceId}: Finding resource groups to delete...", context.InstanceId);
            var resourceGroups = await _azureConnection.ResourceGroups.ListAsync(true);
            _logger.LogDebug("Instance {instanceId}: Found {count} resource groups to delete.", context.InstanceId, resourceGroups.Count());
            return resourceGroups.Select(rg => rg.Name).ToList();
        }

        [FunctionName(nameof(DeleteIfNotLocked))]
        public async Task DeleteIfNotLocked([ActivityTrigger] string name)
        {
            _logger.LogDebug("Checking if resource group {resourceGroup} has locks...", name);
            var locks = await _azureConnection.ManagementLocks.ListByResourceGroupAsync(name, true);

            if (locks.Any())
            {
                foreach (var managementLock in locks)
                {
                    _logger.LogDebug("Lock found in resource group {resourceGroup}, id: {id}, level: {level} notes: {notes}", name, managementLock.Id, managementLock.Level, managementLock.Notes);
                }
                _logger.LogInformation("Resource group {resourceGroup} had at least one lock, skipping deletion.", name);
                return;
            }
            _logger.LogInformation("Deleting resource group {resourceGroup}", name);
            await _azureConnection.ResourceGroups.DeleteByNameAsync(name);
        }
    }
}
