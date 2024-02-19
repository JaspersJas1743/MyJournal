using MyJournal.Core.Utilities;

namespace MyJournal.Core;

public sealed class User
{
	private User()
	{
	}

	public static async Task<User> Create(string token)
	{
		ApiClient.SetToken(token: token);
		// Логика создания пользователя (например, получение данных, дабы не отправлять кучу запросов по необходимости)
		return new User();
	}
}