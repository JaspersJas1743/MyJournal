using Microsoft.Extensions.DependencyInjection;
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

	public async Task<Authorized<User>> SignIn(Credentials<User> credentials, CancellationToken cancellationToken = default(CancellationToken))
	{
		Response response = await client.PostAsync<Response, Credentials<User>>(
			apiMethod: AccountControllerMethods.SignInWithCredentials,
			arg: credentials,
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();

		client.Token = response.Token;
		client.SessionId = response.SessionId;
		User user = response.Role switch
		{
			UserRoles.Teacher => await Teacher.Create(client: client, fileService: fileService, googleAuthenticatorService: googleAuthenticatorService, cancellationToken: cancellationToken),
			UserRoles.Student => await Student.Create(client: client, fileService: fileService, googleAuthenticatorService: googleAuthenticatorService, cancellationToken: cancellationToken),
			UserRoles.Administrator => await Administrator.Create(client: client, fileService: fileService, googleAuthenticatorService: googleAuthenticatorService, cancellationToken: cancellationToken),
			UserRoles.Parent => await Parent.Create(client: client, fileService: fileService, googleAuthenticatorService: googleAuthenticatorService, cancellationToken: cancellationToken),
		};
		return new Authorized<User>(instance: user, typeOfInstance: user.GetType(), token: response.Token);
	}
}

public static class AuthorizationWithCredentialsServiceExtension
{
	public static IServiceCollection AddAuthorizationWithCredentialsService(this IServiceCollection serviceCollection)
		=> serviceCollection.AddTransient<IAuthorizationService<User>, AuthorizationWithCredentialsService>();

	public static IServiceCollection AddKeyedAuthorizationWithCredentialsService(this IServiceCollection serviceCollection, string key)
		=> serviceCollection.AddKeyedTransient<IAuthorizationService<User>, AuthorizationWithCredentialsService>(serviceKey: key);
}