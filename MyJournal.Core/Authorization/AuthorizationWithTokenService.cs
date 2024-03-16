using MyJournal.Core.Utilities;

namespace MyJournal.Core.Authorization;

public class AuthorizationWithTokenService(ApiClient client) : IAuthorizationService<User>
{
	private record Response(int SessionId, bool SessionIsEnabled);

	public async Task<User> SignIn(Credentials<User> credentials, CancellationToken cancellationToken = default(CancellationToken))
	{
		client.Token = credentials.GetCredential<string>(name: nameof(UserTokenCredentials.Token));
		Response response = await client.PostAsync<Response>(
			apiMethod: "account/sign-in/token",
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		client.ResetToken();

		if (!response.SessionIsEnabled)
			throw new InvalidTokenException(message: "Переданный токен не является корректным.");

		return await User.Create(
			client: client,
			sessionId: response.SessionId,
			token: credentials.GetCredential<string>(name: nameof(UserTokenCredentials.Token)),
			cancellationToken: cancellationToken
		);
	}
}