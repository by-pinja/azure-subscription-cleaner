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
            _cleaner = new SubscriptionCleaner(mockLogger);
        }

        [Test]
        public void Run_ThrowsArgumentNullExceptionWithNullTimer()
        {
            var exception = Assert.Throws<System.ArgumentNullException>(() => _cleaner.Run(null));
            Assert.AreEqual("timer", exception.ParamName);
        }
    }
}