using MyJournal.Core.Utilities;

namespace MyJournal.Core.Registration;

public sealed class RegistrationCodeVerificationService(ApiClient client) : IVerificationService<Credentials<User>>
{
	private record Request(string RegistrationCode);
	private record Response(bool IsVerified);

	public async Task<bool> Verify(Credentials<User> credentials, CancellationToken cancellationToken = default(CancellationToken))
	{
		Response response = await client.GetAsync<Response, Request>(
			apiMethod: "account/code/verify",
			argQuery: new Request(RegistrationCode: credentials.GetCredential<string>(name: nameof(Request.RegistrationCode))),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();

		return response.IsVerified;
	}
}