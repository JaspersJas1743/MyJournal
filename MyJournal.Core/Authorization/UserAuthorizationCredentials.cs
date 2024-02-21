using MyJournal.Core.Utilities;

namespace MyJournal.Core.Authorization;

public sealed class UserAuthorizationCredentials(string login, string password, UserAuthorizationCredentials.Clients client) : Credentials<User>
{
	public string Login { get; } = login;
	public string Password { get; } = password;
	public Clients Client { get; } = client;

	public enum Clients
	{
		Windows,
		Linux,
		Chrome,
		Opera,
		Yandex,
		Other
	}
}