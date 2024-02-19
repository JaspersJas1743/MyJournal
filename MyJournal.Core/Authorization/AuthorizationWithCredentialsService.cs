using MyJournal.Core.Utilities;

namespace MyJournal.Core.Authorization;

public sealed class AuthorizationWithCredentialsService(
	string login,
	string password,
	AuthorizationWithCredentialsService.Clients client
) : IAuthorizationService
{
	public enum Clients
	{
		Windows,
		Linux,
		Chrome,
		Opera,
		Yandex,
		Other
	}

	private record Request(string Login, string Password, Clients Client);
	private record Response(string Token);

	public async Task<User> SignIn(CancellationToken cancellationToken = default(CancellationToken))
	{
		Response response = await ApiClient.PostAsync<Response, Request>(
			apiMethod: "Account/SignInWithCredentials",
			arg: new Request(Login: login, Password: password, Client: client),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();

		return await User.Create(token: response.Token);
	}
}