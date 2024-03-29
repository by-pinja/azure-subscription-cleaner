﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Pinja.AzureSubscriptionCleaner.SlackLib;

namespace Pinja.AzureSubscriptionCleaner.AzureFunctions
{
    public class SubscriptionCleaner
    {
        private readonly ILogger<SubscriptionCleaner> _logger;
        private readonly IAzure _azureConnection;
        private readonly ISlackClient _slackClient;
        private readonly CleanupConfiguration _cleanupConfiguration;

        public SubscriptionCleaner(ILogger<SubscriptionCleaner> logger, IAzure azureConnection, ISlackClient slackClient, CleanupConfiguration cleanupConfiguration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _azureConnection = azureConnection ?? throw new ArgumentNullException(nameof(azureConnection));
            _slackClient = slackClient ?? throw new ArgumentNullException(nameof(slackClient));
            _cleanupConfiguration = cleanupConfiguration ?? throw new ArgumentNullException(nameof(cleanupConfiguration));
        }

        [FunctionName(nameof(TimerStart))]
        public async Task TimerStart([TimerTrigger("%CleanupSchedule%")] TimerInfo timer, [DurableClient] IDurableOrchestrationClient starter)
        {
            if (timer is null)
            {
                throw new ArgumentNullException(nameof(timer));
            }

            if (starter is null)
            {
                throw new ArgumentNullException(nameof(starter));
            }

            _logger.LogTrace("{class}, Last execution: {last}", nameof(TimerStart), timer.ScheduleStatus.Last);
            var instanceId = await starter.StartNewAsync(nameof(OchestrateSubscriptionCleanUp), Guid.NewGuid().ToString()).ConfigureAwait(true);
            _logger.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        }

        [FunctionName(nameof(OchestrateSubscriptionCleanUp))]
        public async Task OchestrateSubscriptionCleanUp([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var logger = context.CreateReplaySafeLogger(_logger);
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            logger.LogDebug("Getting resource groups");
            var resourceGroupNames = await context.CallActivityAsync<IEnumerable<string>>(nameof(GetResourceGroupNames), null).ConfigureAwait(true);

            var deletedResourceGroupNames = new List<string>();
            foreach (var name in resourceGroupNames)
            {
                logger.LogDebug("Calling delete function on {resourceGroupName}", name);
                if (await context.CallActivityAsync<bool>(nameof(DeleteIfNotLocked), name).ConfigureAwait(true))
                {
                    deletedResourceGroupNames.Add(name);
                }
            }

            var slackContext = new SlackReportingContext
            {
                DeletedResourceGroups = deletedResourceGroupNames
            };
            await context.CallActivityAsync(nameof(ReportToSlack), slackContext).ConfigureAwait(true);
        }

        [FunctionName(nameof(GetResourceGroupNames))]
        public async Task<IEnumerable<string>> GetResourceGroupNames([ActivityTrigger] IDurableActivityContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
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
                throw new ArgumentException("resource group name is required!", nameof(name));
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
            if (!_cleanupConfiguration.Simulate)
            {
                try
                {
                    /*
                        BeginDeleteByNameAsync is used instead of DeleteByNameAsync because we don't want to wait for operation completion.
                        This may hide errors if deleting fails, but deleting may take longer than 5 minutes (activity function timeout)
                        so we do this to avoid timeout.
                    */
                    await _azureConnection.ResourceGroups.BeginDeleteByNameAsync(name).ConfigureAwait(true);
                }
                catch (Exception exception)
                {
                    /*
                    NOTE: Pokemon pattern is used because we don't have control on the exceptions thrown by 3rd party
                    library and we don't want to stop if execution fails.

                    This can hapend for multiple reasons
                    1. Resources are locked before we delete
                    1. Resources are already deleted before we try to delete
                    1. Some other transient error. Resource group deletion has been
                       known to randomly fail.
                    */
                    _logger.LogError(exception, "Something went wrong while deleting resource group {resourceGroup}", name);
                }
            }

            return true;
        }

        [FunctionName(nameof(ReportToSlack))]
        public async Task ReportToSlack([ActivityTrigger] SlackReportingContext slackReportingContext)
        {
            if (slackReportingContext is null)
            {
                throw new ArgumentNullException(nameof(slackReportingContext));
            }

            var context = new MessageUtil.MessageContext
            {
                DeletedResourceGroups = slackReportingContext.DeletedResourceGroups,
                WasSimulated = _cleanupConfiguration.Simulate
            };
            var message = MessageUtil.CreateDeleteInformationMessage(_cleanupConfiguration.SlackChannel, context);
            await _slackClient.PostMessage(message).ConfigureAwait(false);
        }
    }
}
