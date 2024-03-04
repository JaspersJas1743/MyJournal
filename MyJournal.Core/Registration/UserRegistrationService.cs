using MyJournal.Core.Utilities;

namespace MyJournal.Core.Registration;

public sealed class UserRegistrationService(ApiClient client) : IRegistrationService<User>
{
	private int _userId;

	public record SetPhoneRequest(string NewPhone);
	public record SetEmailRequest(string NewEmail);
	private record SignUpResponse(int Id);
	private record VerifyAuthenticationCodeResponse(bool IsVerified);

	public async Task<bool> Register(
		Credentials<User> credentials,
		IVerificationService<Credentials<User>>? verifier = default(IVerificationService<Credentials<User>>),
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		if (verifier is not null && !await verifier.Verify(toVerifying: credentials, cancellationToken: cancellationToken))
			return false;

		try
		{
			SignUpResponse response = await client.PostAsync<SignUpResponse, Credentials<User>>(
				apiMethod: "account/sign-up",
				arg: credentials,
				cancellationToken: cancellationToken
			) ?? throw new InvalidOperationException();
			_userId = response.Id;
			return true;
		}
		catch (Exception e)
		{
			return false;
		}
	}

	public async Task<IRegistrationService<User>.AuthenticationData> CreateGoogleAuthenticator(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IRegistrationService<User>.AuthenticationData data = await client.GetAsync<IRegistrationService<User>.AuthenticationData>(
			apiMethod: $"account/sign-up/user/{_userId}/code/get",
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return data;
	}

	public async Task<bool> VerifyAuthenticationCode(
		string code,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		VerifyAuthenticationCodeResponse data = await client.GetAsync<VerifyAuthenticationCodeResponse>(
			apiMethod: $"sign-up/user/{_userId}/code/verify",
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return data.IsVerified;
	}

	public async Task SetEmail(string email, CancellationToken cancellationToken = default(CancellationToken))
	{
		await client.PostAsync<SetEmailRequest>(
			apiMethod: $"sign-up/user/{_userId}/email/set",
			arg: new SetEmailRequest(NewEmail: email),
			cancellationToken: cancellationToken
		);
	}

	public async Task SetPhone(string phone, CancellationToken cancellationToken = default(CancellationToken))
	{
		await client.PostAsync<SetPhoneRequest>(
			apiMethod: $"sign-up/user/{_userId}/phone/set",
			arg: new SetPhoneRequest(NewPhone: phone),
			cancellationToken: cancellationToken
		);
	}
}