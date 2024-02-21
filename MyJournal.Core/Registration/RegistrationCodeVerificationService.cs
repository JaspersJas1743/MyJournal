using MyJournal.Core.Utilities;

namespace MyJournal.Core.Registration;

public sealed class RegistrationCodeVerificationService : IVerificationService<Credentials<User>>
{
	private record Response(bool IsVerified);

	public async Task<bool> Verify(Credentials<User> credentials, CancellationToken cancellationToken = default(CancellationToken))
	{
		Response response = await ApiClient.GetAsync<Response, Credentials<User>>(
			apiMethod: "Account/VerifyRegistrationCode",
			argQuery: credentials,
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();

		return response.IsVerified;
	}
}