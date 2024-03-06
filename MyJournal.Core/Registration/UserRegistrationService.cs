using MyJournal.Core.Utilities;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;

namespace MyJournal.Core.Registration;

public sealed class UserRegistrationService(
	ApiClient client,
	IGoogleAuthenticatorService googleAuthenticatorService
) : IRegistrationService<User>
{
	private int _userId = -1;
	private bool _googleAuthenticatorIsCreated = false;
	private bool _googleAuthenticatorIsVerified = false;

	public record SetPhoneRequest(string NewPhone);
	public record SetEmailRequest(string NewEmail);
	private record SignUpResponse(int Id);

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
		if (Int32.IsNegative(value: _userId))
			throw new InvalidOperationException(message: "Перед созданием Google Authenticator необходимо зарегистрировать пользователя.");

		IRegistrationService<User>.AuthenticationData data = await client.GetAsync<IRegistrationService<User>.AuthenticationData>(
			apiMethod: $"account/sign-up/user/{_userId}/code/get",
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		_googleAuthenticatorIsCreated = true;
		return data;
	}

	public async Task<bool> VerifyAuthenticationCode(
		string code,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		if (!_googleAuthenticatorIsCreated)
			throw new InvalidOperationException(message: "Сначала необходимо создать аутентификационный код.");

		_googleAuthenticatorIsVerified = await googleAuthenticatorService.VerifyAuthenticationCode(
			userId: _userId,
			code: code,
			cancellationToken: cancellationToken
		);
		return _googleAuthenticatorIsVerified;
	}

	public async Task SetEmail(string email, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (!_googleAuthenticatorIsVerified)
			throw new InvalidOperationException(message: "Сначала необходимо проверить аутентификационный код.");

		await client.PostAsync<SetEmailRequest>(
			apiMethod: $"account/sign-up/user/{_userId}/email/set",
			arg: new SetEmailRequest(NewEmail: email),
			cancellationToken: cancellationToken
		);
	}

	public async Task SetPhone(string phone, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (!_googleAuthenticatorIsVerified)
			throw new InvalidOperationException(message: "Сначала необходимо проверить аутентификационный код.");

		await client.PostAsync<SetPhoneRequest>(
			apiMethod: $"account/sign-up/user/{_userId}/phone/set",
			arg: new SetPhoneRequest(NewPhone: phone),
			cancellationToken: cancellationToken
		);
	}
}