using MyJournal.Core.Utilities;

namespace MyJournal.Core.Authorization;

public sealed class UserTokenCredentials(string token) : Credentials<User>
{
	public string Token { get; } = token;
}