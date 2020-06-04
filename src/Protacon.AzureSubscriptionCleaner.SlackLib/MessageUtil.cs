using System;
using System.Collections.Generic;
using System.Text;
using Protacon.AzureSubscriptionCleaner.SlackLib.Dto;

namespace Protacon.AzureSubscriptionCleaner.SlackLib
{
    public static class MessageUtil
    {
        public static PostMessage CreateDeleteInformationMessage(string channel, IReadOnlyList<string> deletedResourceGroups, DateTimeOffset? nextTime)
        {
            var messageContent = new StringBuilder();
            messageContent.AppendLine("Following resource groups where deleted in cleanup: ");
            foreach (var deletedResourceGroup in deletedResourceGroups)
            {
                messageContent.AppendLine(deletedResourceGroup);
            }
            if (nextTime.HasValue)
            {
                messageContent.AppendLine($"Next cleanup (UTC): {nextTime}");
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
    }
}