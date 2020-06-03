using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Protacon.AzureSubscriptionCleaner.SlackLib;

namespace Protacon.AzureSubscriptionCleaner.AzureFunctions
{
    public class SubscriptionCleaner
    {
        /// <summary>
        /// We want to avoid executing this during workdays so we'll
        /// execute during weekend
        /// {second} {minute} {hour} {day} {month} {day of the week}
        /// </summary>
        private const string FirstSaturdayOfMonth = "0 0 0 1-7 * SAT";
        private readonly ILogger<SubscriptionCleaner> _logger;
        private readonly IAzure _azureConnection;
        private readonly ISlackClient _slackClient;

        public SubscriptionCleaner(ILogger<SubscriptionCleaner> logger, IAzure azureConnection, ISlackClient slackClient)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
            _azureConnection = azureConnection ?? throw new System.ArgumentNullException(nameof(azureConnection));
            _slackClient = slackClient ?? throw new System.ArgumentNullException(nameof(slackClient));
        }

        [FunctionName(nameof(TimerStart))]
        public async Task TimerStart([TimerTrigger(FirstSaturdayOfMonth)] TimerInfo timer, [DurableClient] IDurableOrchestrationClient starter)
        {
            if (timer is null)
            {
                throw new System.ArgumentNullException(nameof(timer));
            }

            if (starter is null)
            {
                throw new System.ArgumentNullException(nameof(starter));
            }

            _logger.LogTrace("{class}, Next: {next}, Last: {last}", nameof(TimerStart), timer.ScheduleStatus.Next, timer.ScheduleStatus.Last);
            string instanceId = await starter.StartNewAsync(nameof(OchestrateSubscriptionCleanUp), null).ConfigureAwait(true);
            _logger.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        }

        [FunctionName(nameof(OchestrateSubscriptionCleanUp))]
        public async Task OchestrateSubscriptionCleanUp([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            if (context is null)
            {
                throw new System.ArgumentNullException(nameof(context));
            }

            if (!context.IsReplaying)
            {
                _logger.LogDebug("Getting resource groups");
            }
            var resourceGroupNames = await context.CallActivityAsync<IEnumerable<string>>(nameof(GetResourceGroupNames), string.Empty).ConfigureAwait(true);

            var deletedResourceGroupNames = new List<string>();
            foreach (var name in resourceGroupNames)
            {
                if (!context.IsReplaying)
                {
                    _logger.LogDebug("Calling delete function on {resourceGroupName}", name);
                }
                if (await context.CallActivityAsync<bool>(nameof(DeleteIfNotLocked), name).ConfigureAwait(true))
                {
                    deletedResourceGroupNames.Add(name);
                }
            }

            await context.CallActivityAsync(nameof(ReportToSlack), deletedResourceGroupNames).ConfigureAwait(true);
        }

        [FunctionName(nameof(GetResourceGroupNames))]
        public async Task<IEnumerable<string>> GetResourceGroupNames([ActivityTrigger] IDurableActivityContext context)
        {
            if (context is null)
            {
                throw new System.ArgumentNullException(nameof(context));
            }

            _logger.LogTrace("Instance {instanceId}: Finding resource groups to delete...", context.InstanceId);
            var resourceGroups = await _azureConnection.ResourceGroups.ListAsync(true).ConfigureAwait(true);
            _logger.LogDebug("Instance {instanceId}: Found {count} resource groups to delete.", context.InstanceId, resourceGroups.Count());
            return resourceGroups.Select(rg => rg.Name).ToList();
        }

        [FunctionName(nameof(DeleteIfNotLocked))]
        public async Task<bool> DeleteIfNotLocked([ActivityTrigger] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new System.ArgumentException("resource group name is required!", nameof(name));
            }

            _logger.LogDebug("Checking if resource group {resourceGroup} has locks...", name);
            var locks = await _azureConnection.ManagementLocks.ListByResourceGroupAsync(name, true).ConfigureAwait(true);

            if (locks.Any())
            {
                foreach (var managementLock in locks)
                {
                    _logger.LogDebug("Lock found in resource group {resourceGroup}, id: {id}, level: {level} notes: {notes}", name, managementLock.Id, managementLock.Level, managementLock.Notes);
                }
                _logger.LogInformation("Resource group {resourceGroup} had at least one lock, skipping deletion.", name);
                return false;
            }
            _logger.LogInformation("Deleting resource group {resourceGroup}", name);
            await _azureConnection.ResourceGroups.DeleteByNameAsync(name).ConfigureAwait(true);
            return true;
        }

        [FunctionName(nameof(ReportToSlack))]
        public async Task ReportToSlack([ActivityTrigger] IReadOnlyList<string> deletedResourceGroups)
        {
            var message = MessageUtil.CreateDeleteInformationMessage("hjni-testi", deletedResourceGroups);
            await _slackClient.PostMessage(message).ConfigureAwait(false);
        }
    }
}
