using MyJournal.Core.Utilities;
using MyJournal.Core.Utilities.Constants;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;

namespace MyJournal.Core.Authorization;

public class AuthorizationWithTokenService(
	ApiClient client,
	IGoogleAuthenticatorService googleAuthenticatorService
) : IAuthorizationService<User>
{
	private record Response(int SessionId, bool SessionIsEnabled, UserRoles Role);

	private enum UserRoles
	{
		Student,
		Teacher,
		Administrator,
		Parent
	}

	public async Task<User> SignIn(Credentials<User> credentials, CancellationToken cancellationToken = default(CancellationToken))
	{
		client.Token = credentials.GetCredential<string>(name: nameof(UserTokenCredentials.Token));
		Response response = await client.PostAsync<Response>(
			apiMethod: AccountControllerMethods.SignInWithToken,
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();

		if (!response.SessionIsEnabled)
			throw new InvalidTokenException(message: "Данная сессия завершена.");

		client.SessionId = response.SessionId;
		return response.Role switch
		{
			UserRoles.Teacher => await Teacher.Create(client: client, googleAuthenticatorService: googleAuthenticatorService, cancellationToken: cancellationToken),
			UserRoles.Student => await Student.Create(client: client, googleAuthenticatorService: googleAuthenticatorService, cancellationToken: cancellationToken),
			UserRoles.Administrator => await Administrator.Create(client: client, googleAuthenticatorService: googleAuthenticatorService, cancellationToken: cancellationToken),
			UserRoles.Parent => await Parent.Create(client: client, googleAuthenticatorService: googleAuthenticatorService, cancellationToken: cancellationToken),
		};
	}
}