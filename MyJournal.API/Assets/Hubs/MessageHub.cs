using Microsoft.AspNetCore.SignalR;

namespace MyJournal.API.Assets.Hubs;

public class MessageHub : Hub<IMessageHubClient>
{
	public async Task Send(string message)
		=> await Clients.All.SendOffersToUser(message);
}