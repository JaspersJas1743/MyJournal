using Microsoft.Extensions.DependencyInjection;
using MyJournal.Core.Utilities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.Registration;

public sealed class LoginVerificationService(ApiClient client) : IVerificationService<Credentials<User>>
{
	private record Request(string Login);
	private record Response(bool IsVerified);

	public async Task<bool> Verify(Credentials<User> credentials, CancellationToken cancellationToken = default(CancellationToken))
	{
		Response response = await client.GetAsync<Response, Request>(
			apiMethod: AccountControllerMethods.VerifyLogin,
			argQuery: new Request(Login: credentials.GetCredential<string>(name: nameof(Request.Login))),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();

		return response.IsVerified;
	}
}

public static class LoginVerificationServiceExtension
{
	public static IServiceCollection AddLoginVerificationService(this IServiceCollection serviceCollection)
		=> serviceCollection.AddTransient<IVerificationService<Credentials<User>>, LoginVerificationService>();

	public static IServiceCollection AddKeyedLoginVerificationService(this IServiceCollection serviceCollection, string key)
		=> serviceCollection.AddKeyedTransient<IVerificationService<Credentials<User>>, LoginVerificationService>(serviceKey: key);
}