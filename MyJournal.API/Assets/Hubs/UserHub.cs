using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MyJournal.API.Assets.Hubs;

[Authorize]
public class UserHub : Hub<IUserHub>
{
	// TODO: доработать (добавить группы), дабы не извещать тех, кого не нужно
	public async Task SetOnline(int userId, DateTime onlineAt)
		=> await Clients.All.SetOnline(userId: userId, onlineAt);

	public async Task SetOffline(int userId, DateTime onlineAt)
		=> await Clients.All.SetOffline(userId: userId, onlineAt);

	public async Task UpdatedProfilePhoto(int userId)
		=> await Clients.All.UpdatedProfilePhoto(userId: userId);

	public async Task DeletedProfilePhoto(int userId)
		=> await Clients.All.DeletedProfilePhoto(userId: userId);

	public async Task CreateChat(int userId)
		=> await Clients.All.CreateChat(userId: userId);
}