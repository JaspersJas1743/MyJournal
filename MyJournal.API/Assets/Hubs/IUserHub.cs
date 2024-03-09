namespace MyJournal.API.Assets.Hubs;

public interface IUserHub
{
	Task SetOnline(int userId, DateTime? onlineAt);
	Task SetOffline(int userId, DateTime? onlineAt);
	Task UpdatedProfilePhoto(int userId);
	Task DeletedProfilePhoto(int userId);
	Task CreateChat(int userId);
}