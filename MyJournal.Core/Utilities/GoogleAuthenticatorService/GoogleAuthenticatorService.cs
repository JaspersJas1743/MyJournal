using Microsoft.Extensions.DependencyInjection;

namespace MyJournal.Core.Utilities.GoogleAuthenticatorService;

public class GoogleAuthenticatorService(ApiClient client) : IGoogleAuthenticatorService
{
	private record VerifyAuthenticationCodeRequest(string UserCode);
	private record VerifyAuthenticationCodeResponse(bool IsVerified);

	public async Task<bool> VerifyAuthenticationCode(
		int userId,
		string code,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		VerifyAuthenticationCodeResponse data = await client.GetAsync<VerifyAuthenticationCodeResponse, VerifyAuthenticationCodeRequest>(
			apiMethod: $"account/user/{userId}/code/verify",
			argQuery: new VerifyAuthenticationCodeRequest(UserCode: code),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return data.IsVerified;
	}
}

public static class GoogleAuthenticatorServiceExtensions
{
	public static void AddGoogleAuthenticator(this IServiceCollection serviceCollection)
		=> serviceCollection.AddTransient<IGoogleAuthenticatorService, GoogleAuthenticatorService>();
}