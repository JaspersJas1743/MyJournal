using MyJournal.Core.Utilities;
using MyJournal.Core.Utilities.Constants;

namespace MyJournal.Core.Registration;

public sealed class RegistrationCodeVerificationService(ApiClient client) : IVerificationService<Credentials<User>>
{
	private record Request(string RegistrationCode);
	private record Response(bool IsVerified);

	public async Task<bool> Verify(Credentials<User> credentials, CancellationToken cancellationToken = default(CancellationToken))
	{
		Response response = await client.GetAsync<Response, Request>(
			apiMethod: AccountControllerMethods.VerifyRegistrationCode,
			argQuery: new Request(RegistrationCode: credentials.GetCredential<string>(name: nameof(Request.RegistrationCode))),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();

		return response.IsVerified;
	}
}