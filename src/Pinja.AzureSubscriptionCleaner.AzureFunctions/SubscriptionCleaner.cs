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
        /// <summary>
        /// We want to avoid executing this during workdays so we'll
        /// execute during weekend
        /// {second} {minute} {hour} {day} {month} {day of the week}
        /// </summary>
        private const string FirstSaturdayOfMonth = "0 0 0 1-7 * SAT";
        private readonly ILogger<SubscriptionCleaner> _logger;
        private readonly IAzure _azureConnection;
        private readonly ISlackClient _slackClient;
        private readonly ReportingConfiguration _reportingConfiguration;

        public SubscriptionCleaner(ILogger<SubscriptionCleaner> logger, IAzure azureConnection, ISlackClient slackClient, ReportingConfiguration reportingConfiguration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _azureConnection = azureConnection ?? throw new ArgumentNullException(nameof(azureConnection));
            _slackClient = slackClient ?? throw new ArgumentNullException(nameof(slackClient));
            _reportingConfiguration = reportingConfiguration ?? throw new ArgumentNullException(nameof(reportingConfiguration));
        }

        [FunctionName(nameof(TimerStart))]
        public async Task TimerStart([TimerTrigger(FirstSaturdayOfMonth)] TimerInfo timer, [DurableClient] IDurableOrchestrationClient starter)
        {
            if (timer is null)
            {
                throw new ArgumentNullException(nameof(timer));
            }

            if (starter is null)
            {
                throw new ArgumentNullException(nameof(starter));
            }

            _logger.LogTrace("{class}, Next: {next}, Last: {last}", nameof(TimerStart), timer.ScheduleStatus.Next, timer.ScheduleStatus.Last);
            string instanceId = await starter.StartNewAsync(nameof(OchestrateSubscriptionCleanUp), Guid.NewGuid().ToString(), timer.ScheduleStatus.Next).ConfigureAwait(true);
            _logger.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        }

        [FunctionName(nameof(OchestrateSubscriptionCleanUp))]
        public async Task OchestrateSubscriptionCleanUp([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
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

            var slackContext = new SlackReportingContext
            {
                NextOccurrence = context.GetInput<DateTime>(),
                DeletedResourceGroups = deletedResourceGroupNames
            };
            await context.CallActivityAsync(nameof(ReportToSlack), slackContext).ConfigureAwait(true);
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
        public async Task ReportToSlack([ActivityTrigger] SlackReportingContext slackReportingContext)
        {
            if (slackReportingContext is null)
            {
                throw new ArgumentNullException(nameof(slackReportingContext));
            }

            var context = new MessageUtil.MessageContext
            {
                DeletedResourceGroups = slackReportingContext.DeletedResourceGroups,
                NextTime = slackReportingContext.NextOccurrence,
            };
            var message = MessageUtil.CreateDeleteInformationMessage(_reportingConfiguration.SlackChannel, context);
            await _slackClient.PostMessage(message).ConfigureAwait(false);
        }
    }
}