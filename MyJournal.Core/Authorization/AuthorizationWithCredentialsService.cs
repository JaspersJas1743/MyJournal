using MyJournal.Core.Utilities;

namespace MyJournal.Core.Authorization;

public sealed class AuthorizationWithCredentialsService(ApiClient client) : IAuthorizationService<User>
{
	private record Response(string Token);

	public async Task<User> SignIn(Credentials<User> credentials, CancellationToken cancellationToken = default(CancellationToken))
	{
		Response response = await client.PostAsync<Response, Credentials<User>>(
			apiMethod: "account/sign-in/credentials",
			arg: credentials,
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();

		return await User.Create(client: client, token: response.Token, cancellationToken: cancellationToken);
	}
}