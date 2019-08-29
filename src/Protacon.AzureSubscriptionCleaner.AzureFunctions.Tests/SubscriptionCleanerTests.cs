using Microsoft.Azure.Management.Fluent;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace Protacon.AzureSubscriptionCleaner.AzureFunctions.Tests
{
    public class SubscriptionCleanerTests
    {
        private SubscriptionCleaner _cleaner;

        [SetUp]
        public void Setup()
        {
            var mockLogger = Substitute.For<ILogger<SubscriptionCleaner>>();
            var azure = Substitute.For<IAzure>();
            _cleaner = new SubscriptionCleaner(mockLogger, azure);
        }

        [Test]
        public void StartMonitoring_ThrowsArgumentNullExceptionWithNullTimer()
        {
            var exception = Assert.ThrowsAsync<System.ArgumentNullException>(() => _cleaner.StartMonitoring(null));
            Assert.AreEqual("context", exception.ParamName);
        }
    }
}