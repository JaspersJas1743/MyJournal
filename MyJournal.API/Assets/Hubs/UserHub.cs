using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MyJournal.API.Assets.Hubs;

[Authorize]
public class UserHub : Hub<IUserHub>
{
	public async Task SetOnline(int userId, DateTime onlineAt)
		=> await Clients.All.SetOnline(userId: userId, onlineAt);

	public async Task SetOffline(int userId, DateTime onlineAt)
		=> await Clients.All.SetOffline(userId: userId, onlineAt);

	public async Task UpdatedProfilePhoto(int userId)
		=> await Clients.All.UpdatedProfilePhoto(userId: userId);
}