using System.Threading.Tasks;

namespace MyJournal.API.Assets.Hubs;

public interface IMessageHubClient
{
	Task SendOffersToUser(string message);
}