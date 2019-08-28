using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Protacon.AzureSubscriptionCleaner.AzureFunctions
{
    public class SubscriptionCleaner
    {
        private readonly ILogger<SubscriptionCleaner> _logger;

        public SubscriptionCleaner(ILogger<SubscriptionCleaner> logger)
        {
            _logger = logger;
        }

        [FunctionName("SubscriptionCleaner")]
        public void Run([TimerTrigger("0 0 0 * * *")]TimerInfo timer)
        {
            if (timer == null)
            {
                throw new ArgumentNullException(nameof(timer));
            }
            _logger.LogTrace("{class}, Next: {next}, Last: {last}", nameof(SubscriptionCleaner), timer.ScheduleStatus.Next, timer.ScheduleStatus.Last);
        }
    }
}
