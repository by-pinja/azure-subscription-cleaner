using System.Threading.Tasks;
using Protacon.AzureSubscriptionCleaner.SlackLib.Dto;

namespace Protacon.AzureSubscriptionCleaner.SlackLib
{
    public interface ISlackClient
    {
        Task<PostMessageResponse> PostMessage(PostMessage message);
    }
}