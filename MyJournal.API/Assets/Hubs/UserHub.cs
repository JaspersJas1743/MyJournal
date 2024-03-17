using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MyJournal.API.Assets.Hubs;

[Authorize]
public class UserHub : Hub<IUserHub>
{
	public async Task SetOnline(int userId, DateTime onlineAt, IEnumerable<string> interlocutorIds)
		=> await Clients.Users(userIds: interlocutorIds).SetOnline(userId: userId, onlineAt);

	public async Task SetOffline(int userId, DateTime onlineAt, IEnumerable<string> interlocutorIds)
		=> await Clients.Users(userIds: interlocutorIds).SetOffline(userId: userId, onlineAt);

	public async Task SignIn()
		=> await Clients.Caller.SignIn();

	public async Task SignOut(IEnumerable<int> sessionIds)
		=> await Clients.Caller.SignOut(sessionIds: sessionIds);

	public async Task UpdatedProfilePhoto(int userId, IEnumerable<string> interlocutorIds)
		=> await Clients.Users(userIds: interlocutorIds).UpdatedProfilePhoto(userId: userId);

	public async Task DeletedProfilePhoto(int userId, IEnumerable<string> interlocutorIds)
		=> await Clients.Users(userIds: interlocutorIds).DeletedProfilePhoto(userId: userId);

	public async Task CreatedChat(int chatId, IEnumerable<string> interlocutorIds)
		=> await Clients.Users(userIds: interlocutorIds).JoinedInChat(id: chatId);

	public async Task SetPhone(string phone, IEnumerable<string> interlocutorIds)
		=> await Clients.Users(userIds: interlocutorIds).SetPhone(phone: phone);

	public async Task SetEmail(string email, IEnumerable<string> interlocutorIds)
		=> await Clients.Users(userIds: interlocutorIds).SetEmail(email: email);
}