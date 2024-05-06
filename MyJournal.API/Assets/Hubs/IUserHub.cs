namespace MyJournal.API.Assets.Hubs;

public interface IUserHub
{
	Task SetOnline(int userId, DateTime? onlineAt);
	Task SetOffline(int userId, DateTime? onlineAt);

	Task UpdatedProfilePhoto(int userId, string link);
	Task DeletedProfilePhoto(int userId);

	Task SignIn(int sessionId);
	Task SignOut(IEnumerable<int> sessionIds);

	Task JoinedInChat(int id);

	Task SetPhone(string? phone);
	Task SetEmail(string? email);

	Task SendMessage(int chatId, int messageId);
	Task ReadChat(int chatId);
}