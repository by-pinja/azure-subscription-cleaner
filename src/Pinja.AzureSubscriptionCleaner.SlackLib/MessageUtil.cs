using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pinja.AzureSubscriptionCleaner.SlackLib.Dto;

namespace Pinja.AzureSubscriptionCleaner.SlackLib
{
    public static class MessageUtil
    {
        public static PostMessage CreateDeleteInformationMessage(string channel, MessageContext messageContext)
        {
            var messageContent = new StringBuilder();
            if (messageContext.WasSimulated)
            {
                messageContent.AppendLine("*THIS IS A SIMULATION*, no changes were made.");
            }

            if (messageContext.DeletedResourceGroups.Any())
            {
                messageContent.AppendLine("Following resource groups were deleted in cleanup: ");
                foreach (var deletedResourceGroup in messageContext.DeletedResourceGroups)
                {
                    messageContent.AppendLine(deletedResourceGroup);
                }
            }
            else
            {
                messageContent.AppendLine("No resource groups were deleted.");
            }

            if (messageContext.NextTime.HasValue)
            {
                messageContent.AppendLine($"Next cleanup (UTC): {messageContext.NextTime}");
            }

            var message = new PostMessage
            {
                Channel = channel,
                Blocks = new Section[]
                {
                    new Section
                    {
                        Text = new TextDto
                        {
                            Text = messageContent.ToString(),
                            Type = "mrkdwn"
                        }
                    }
                }
            };
            return message;
        }

        public class MessageContext
        {
            public IReadOnlyList<string> DeletedResourceGroups { get; set; }
            public DateTimeOffset? NextTime { get; set; }
            public bool WasSimulated { get; set; }
        }
    }
}
