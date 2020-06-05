using System.Threading.Tasks;
using Pinja.AzureSubscriptionCleaner.SlackLib.Dto;

namespace Pinja.AzureSubscriptionCleaner.SlackLib
{
    public interface ISlackClient
    {
        Task<PostMessageResponse> PostMessage(PostMessage message);
    }
}