using Microsoft.Extensions.DependencyInjection;
using MyJournal.Core.UserData;
using MyJournal.Core.Utilities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.FileService;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;

namespace MyJournal.Core.Authorization;

public class AuthorizationWithTokenService(
	ApiClient client,
	IGoogleAuthenticatorService googleAuthenticatorService,
	IFileService fileService
) : IAuthorizationService<User>
{
	private record Response(int SessionId, bool SessionIsEnabled, UserRoles Role);

	public async Task<Authorized<User>> SignIn(Credentials<User> credentials, CancellationToken cancellationToken = default(CancellationToken))
	{
		string token = credentials.GetCredential<string>(name: nameof(UserTokenCredentials.Token));
		client.Token = token;
		Response response = await client.PostAsync<Response>(
			apiMethod: AccountControllerMethods.SignInWithToken,
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();

		if (!response.SessionIsEnabled)
			throw new InvalidTokenException(message: "Данная сессия завершена.");

		client.SessionId = response.SessionId;
		User user = response.Role switch
		{
			UserRoles.Teacher => await Teacher.Create(client: client, fileService: fileService, googleAuthenticatorService: googleAuthenticatorService, cancellationToken: cancellationToken),
			UserRoles.Student => await Student.Create(client: client, fileService: fileService, googleAuthenticatorService: googleAuthenticatorService, cancellationToken: cancellationToken),
			UserRoles.Administrator => await Administrator.Create(client: client, fileService: fileService, googleAuthenticatorService: googleAuthenticatorService, cancellationToken: cancellationToken),
			UserRoles.Parent => await Parent.Create(client: client, fileService: fileService, googleAuthenticatorService: googleAuthenticatorService, cancellationToken: cancellationToken),
		};
		Activity activity = await user.GetActivity();
		await activity.SetOnline(cancellationToken: cancellationToken);
		return new Authorized<User>(instance: user, typeOfInstance: user.GetType(), token: token, role: response.Role);
	}
}

public static class AuthorizationWithTokenServiceExtension
{
	public static IServiceCollection AddAuthorizationWithTokenService(this IServiceCollection serviceCollection)
		=> serviceCollection.AddTransient<IAuthorizationService<User>, AuthorizationWithTokenService>();

	public static IServiceCollection AddKeyedAuthorizationWithTokenService(this IServiceCollection serviceCollection, string key)
		=> serviceCollection.AddKeyedTransient<IAuthorizationService<User>, AuthorizationWithTokenService>(serviceKey: key);
}