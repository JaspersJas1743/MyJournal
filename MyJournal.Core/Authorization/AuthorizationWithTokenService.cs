using MyJournal.Core.Utilities;

namespace MyJournal.Core.Authorization;

public class AuthorizationWithTokenService : IAuthorizationService<User>
{
	private record Response(bool SessionIsEnabled);

	public async Task<User> SignIn(Credentials<User> credentials, CancellationToken cancellationToken = default(CancellationToken))
	{
		ApiClient.Token = credentials.GetCredential<string>(name: nameof(UserTokenCredentials.Token));
		Response response = await ApiClient.PostAsync<Response>(
			apiMethod: "Account/SignInWithToken",
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		ApiClient.ResetToken();

		if (!response.SessionIsEnabled)
			throw new InvalidTokenException(message: "Переданный токен не является корректным.");

		return await User.Create(token: credentials.GetCredential<string>(name: nameof(UserTokenCredentials.Token)));
	}
}