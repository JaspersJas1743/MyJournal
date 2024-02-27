using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MyJournal.API.Assets.Hubs;

[Authorize]
public class UserHub : Hub<IUserHub>
{
	public async Task SetOnline(int userId)
		=> await Clients.All.SetOnline(userId: userId);

	public async Task SetOffline(int userId)
		=> await Clients.All.SetOffline(userId: userId);
}