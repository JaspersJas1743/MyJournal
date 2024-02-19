using MyJournal.Core.Utilities;

namespace MyJournal.Core.Authorization;

public class AuthorizationWithTokenService(
	string token
) : IAuthorizationService
{
	private record Request(string Token);
	private record Response(bool SessionIsEnabled);

	public async Task<User> SignIn(CancellationToken cancellationToken = default(CancellationToken))
	{
		Response response = await ApiClient.PostAsync<Response, Request>(
			apiMethod: "Account/SignInWithToken",
			arg: new Request(Token: token),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();

		if (!response.SessionIsEnabled)
			throw new InvalidTokenException(message: "Переданный токен не является корректным");

		return await User.Create(token: token);
	}
}