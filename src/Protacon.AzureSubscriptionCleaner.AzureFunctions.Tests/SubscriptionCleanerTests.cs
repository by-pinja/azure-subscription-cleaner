using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Locks.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace Protacon.AzureSubscriptionCleaner.AzureFunctions.Tests
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
            _cleaner = new SubscriptionCleaner(mockLogger, _mockAzure);
        }

        [Test]
        public async Task StartMonitoring_CallsStuff()
        {
            var mockContext = Substitute.For<IDurableOrchestrationContext>();

            var groups = new List<string>()
            {
                "group1",
                "group2"
            };

            mockContext.CallActivityAsync<IEnumerable<string>>("GetResourceGroupsNames", null).Returns(Task.FromResult((IEnumerable<string>)groups));

            await _cleaner.StartMonitoring(mockContext);

            await mockContext.Received().CallActivityAsync("DeleteIfNotLocked", groups[0]);
            await mockContext.Received().CallActivityAsync("DeleteIfNotLocked", groups[1]);
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

            var result = await _cleaner.GetResourceGroupsNames(mockContext);
            Assert.AreEqual(2, result.Count());
        }

        [Test]
        public async Task DeleteIfNotLocked_DoesntTryToRemoveLockedGroups()
        {
            const string expectedGroup = "Group";

            var managementLock = Substitute.For<IManagementLock>();
            var paged = new PagedCollection<IManagementLock>
            {
                managementLock
            };
            _mockAzure.ManagementLocks.ListByResourceGroupAsync(expectedGroup, true).Returns(paged);

            await _cleaner.DeleteIfNotLocked(expectedGroup);

            await _mockAzure.ResourceGroups.DidNotReceive().DeleteByNameAsync(Arg.Any<string>());
        }

        [Test]
        public async Task DeleteIfNotLocked_DeletesNonLocked()
        {
            const string expectedGroup = "Group";
            _mockAzure.ManagementLocks
                .ListByResourceGroupAsync(expectedGroup, true)
                .Returns(new PagedCollection<IManagementLock>());

            await _cleaner.DeleteIfNotLocked(expectedGroup);

            await _mockAzure.ResourceGroups.Received().DeleteByNameAsync(expectedGroup);
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