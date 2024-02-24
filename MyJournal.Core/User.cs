using MyJournal.Core.Utilities;

namespace MyJournal.Core;

public sealed class User
{
	private User() { }

	#region Records
	private record SignOutResponse(string Message);
	#endregion

	#region Enums

	private enum SignOutOptions
	{
		SignOut,
		SignOutAll,
		SignOutAllExceptThis
	}
	#endregion

	public static async Task<User> Create(string token)
	{
		ApiClient.SetToken(token: token);
		// Логика создания пользователя (например, получение данных, дабы не отправлять кучу запросов по необходимости)
		return new User();
	}

	private async Task<string> SignOut(SignOutOptions options, CancellationToken cancellationToken = default(CancellationToken))
	{
		SignOutResponse response = await ApiClient.PostAsync<SignOutResponse>(
			apiMethod: $"Account/{options.ToString()}",
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();

		return response.Message;
	}

	public async Task<string> SignOut(CancellationToken cancellationToken = default(CancellationToken))
		=> await SignOut(options: SignOutOptions.SignOut, cancellationToken: cancellationToken);

	public async Task<string> SignOutAll(CancellationToken cancellationToken = default(CancellationToken))
		=> await SignOut(options: SignOutOptions.SignOutAll, cancellationToken: cancellationToken);

	public async Task<string> SignOutAllExceptThis(CancellationToken cancellationToken = default(CancellationToken))
		=> await SignOut(options: SignOutOptions.SignOutAllExceptThis, cancellationToken: cancellationToken);
}