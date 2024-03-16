using MyJournal.Core.Utilities;
using MyJournal.Core.Utilities.Constants;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;

namespace MyJournal.Core.Authorization;

public sealed class AuthorizationWithCredentialsService(
	ApiClient client,
	IGoogleAuthenticatorService googleAuthenticatorService
) : IAuthorizationService<User>
{
	private record Response(int SessionId, string Token);

	public async Task<User> SignIn(Credentials<User> credentials, CancellationToken cancellationToken = default(CancellationToken))
	{
		Response response = await client.PostAsync<Response, Credentials<User>>(
			apiMethod: AccountControllerMethods.SignInWithCredentials,
			arg: credentials,
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();

		return await User.Create(
			client: client,
			googleAuthenticatorService: googleAuthenticatorService,
			sessionId: response.SessionId,
			token: response.Token,
			cancellationToken: cancellationToken
		);
	}
}