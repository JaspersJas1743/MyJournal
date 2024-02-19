using MyJournal.Core.Utilities;

namespace MyJournal.Core.Registration;

public sealed class RegistrationCodeVerificationService : IVerificationService<Credentials<User>>
{
	private record Request(string RegistrationCode);
	private record Response(bool IsVerified);

	public async Task<bool> Verify(Credentials<User> credentials, CancellationToken cancellationToken = default(CancellationToken))
	{
		Response response = await ApiClient.GetAsync<Request, Response>(
			apiMethod: "Account/VerifyRegistrationCode",
			argQuery: new Request(RegistrationCode: credentials.GetCredential(name: "RegistrationCode")!),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();

		return response.IsVerified;
	}
}