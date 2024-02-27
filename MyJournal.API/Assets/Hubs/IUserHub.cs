namespace MyJournal.API.Assets.Hubs;

public interface IUserHub
{
	Task SetOnline(int userId);
	Task SetOffline(int userId);
}