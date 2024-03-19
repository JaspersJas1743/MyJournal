using MyJournal.Core.Utilities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.FileService;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;

namespace MyJournal.Core.Authorization;

public sealed class AuthorizationWithCredentialsService(
	ApiClient client,
	IGoogleAuthenticatorService googleAuthenticatorService,
	IFileService fileService
) : IAuthorizationService<User>
{
	private record Response(int SessionId, string Token, UserRoles Role);

	private enum UserRoles
	{
		Student,
		Teacher,
		Administrator,
		Parent
	}

	public async Task<User> SignIn(Credentials<User> credentials, CancellationToken cancellationToken = default(CancellationToken))
	{
		Response response = await client.PostAsync<Response, Credentials<User>>(
			apiMethod: AccountControllerMethods.SignInWithCredentials,
			arg: credentials,
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();

		client.Token = response.Token;
		client.SessionId = response.SessionId;
		return response.Role switch
		{
			UserRoles.Teacher => await Teacher.Create(client: client, fileService: fileService, googleAuthenticatorService: googleAuthenticatorService, cancellationToken: cancellationToken),
			UserRoles.Student => await Student.Create(client: client, fileService: fileService, googleAuthenticatorService: googleAuthenticatorService, cancellationToken: cancellationToken),
			UserRoles.Administrator => await Administrator.Create(client: client, fileService: fileService, googleAuthenticatorService: googleAuthenticatorService, cancellationToken: cancellationToken),
			UserRoles.Parent => await Parent.Create(client: client, fileService: fileService, googleAuthenticatorService: googleAuthenticatorService, cancellationToken: cancellationToken),
		};
	}
}