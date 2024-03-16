using MyJournal.Core.Utilities;

namespace MyJournal.Core.Authorization;

public sealed class AuthorizationWithCredentialsService(ApiClient client) : IAuthorizationService<User>
{
	private record Response(int SessionId, string Token);

	public async Task<User> SignIn(Credentials<User> credentials, CancellationToken cancellationToken = default(CancellationToken))
	{
		Response response = await client.PostAsync<Response, Credentials<User>>(
			apiMethod: "account/sign-in/credentials",
			arg: credentials,
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();

		return await User.Create(
			client: client,
			sessionId: response.SessionId,
			token: response.Token,
			cancellationToken: cancellationToken
		);
	}
}