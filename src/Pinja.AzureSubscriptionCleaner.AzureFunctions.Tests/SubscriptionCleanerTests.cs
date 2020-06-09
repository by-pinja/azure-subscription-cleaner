using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Locks.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Pinja.AzureSubscriptionCleaner.SlackLib;

namespace Pinja.AzureSubscriptionCleaner.AzureFunctions.Tests
{
    public class SubscriptionCleanerTests
    {
        private SubscriptionCleaner _cleaner;
        private IAzure _mockAzure;

        [SetUp]
        public void Setup()
        {
            var mockLogger = Substitute.For<ILogger<SubscriptionCleaner>>();
            _mockAzure = Substitute.For<IAzure>();
            _cleaner = new SubscriptionCleaner(mockLogger, _mockAzure, Substitute.For<ISlackClient>(), Substitute.For<CleanupConfiguration>());
        }

        [Test]
        public async Task TimerStart_CallsDoMonitoring()
        {
            var timer = new TimerInfo(new MockTimerSchedule(), new ScheduleStatus());
            var starter = Substitute.For<IDurableOrchestrationClient>();
            await _cleaner.TimerStart(timer, starter);

            await starter.Received().StartNewAsync(nameof(SubscriptionCleaner.OchestrateSubscriptionCleanUp), Arg.Any<string>(), Arg.Any<DateTime>());
        }

        [Test]
        public async Task DoMonitoring_CallsStuff()
        {
            var mockContext = Substitute.For<IDurableOrchestrationContext>();
            mockContext.GetInput<DateTime>().Returns(DateTime.UtcNow);

            var groups = new List<string>()
            {
                "group1",
                "group2"
            };

            mockContext.CallActivityAsync<IEnumerable<string>>(nameof(SubscriptionCleaner.GetResourceGroupNames), string.Empty).Returns(Task.FromResult((IEnumerable<string>)groups));
            mockContext.CallActivityAsync<bool>(nameof(SubscriptionCleaner.DeleteIfNotLocked), groups[0]).Returns(Task.FromResult(true));
            mockContext.CallActivityAsync<bool>(nameof(SubscriptionCleaner.DeleteIfNotLocked), groups[1]).Returns(Task.FromResult(true));

            await _cleaner.OchestrateSubscriptionCleanUp(mockContext);

            await mockContext.Received().CallActivityAsync<bool>(nameof(SubscriptionCleaner.DeleteIfNotLocked), groups[0]);
            await mockContext.Received().CallActivityAsync<bool>(nameof(SubscriptionCleaner.DeleteIfNotLocked), groups[1]);
            await mockContext.Received().CallActivityAsync(nameof(SubscriptionCleaner.ReportToSlack), Arg.Any<SlackReportingContext>());
        }

        [Test]
        public async Task GetResourceGroupsNames_ReturnsResourceGroupNames()
        {
            var mockContext = Substitute.For<IDurableActivityContext>();

            var group1 = Substitute.For<IResourceGroup>();
            group1.Name.Returns("Group1");
            var group2 = Substitute.For<IResourceGroup>();
            group2.Name.Returns("Group2");

            var paged = new PagedCollection<IResourceGroup>
            {
                group1,
                group2
            };

            _mockAzure.ResourceGroups.ListAsync(true).Returns(paged);

            var result = await _cleaner.GetResourceGroupNames(mockContext);
            Assert.AreEqual(2, result.Count());
        }

        [Test]
        public async Task DeleteIfNotLocked_DoesntTryToRemoveLockedGroups()
        {
            const string ExpectedGroup = "Group";

            var managementLock = Substitute.For<IManagementLock>();
            var paged = new PagedCollection<IManagementLock>
            {
                managementLock
            };
            _mockAzure.ManagementLocks.ListByResourceGroupAsync(ExpectedGroup, true).Returns(paged);

            await _cleaner.DeleteIfNotLocked(ExpectedGroup);

            await _mockAzure.ResourceGroups.DidNotReceive().DeleteByNameAsync(Arg.Any<string>());
        }

        [Test]
        public async Task DeleteIfNotLocked_DeletesNonLocked()
        {
            const string ExpectedGroup = "Group";
            _mockAzure.ManagementLocks
                .ListByResourceGroupAsync(ExpectedGroup, true)
                .Returns(new PagedCollection<IManagementLock>());

            await _cleaner.DeleteIfNotLocked(ExpectedGroup);

            await _mockAzure.ResourceGroups.Received().DeleteByNameAsync(ExpectedGroup);
        }

        private class MockTimerSchedule : TimerSchedule
        {
            public override DateTime GetNextOccurrence(DateTime now)
            {
                return DateTime.UtcNow;
            }
        }

        private class PagedCollection<T> : List<T>, IPagedCollection<T>
        {
            public int MaxItems { get; set; }

            public Task<IPagedCollection<T>> GetNextPageAsync(CancellationToken cancellationToken = default)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}