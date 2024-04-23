using Microsoft.Extensions.DependencyInjection;
using MyJournal.Core.Utilities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;

namespace MyJournal.Core.RestoringAccess;

public class RestoringAccessThroughEmailService(
	ApiClient client,
	IGoogleAuthenticatorService googleAuthenticatorService
) : IRestoringAccessService<User>
{
	private int _userId = -1;
	private bool _googleAuthenticatorIsVerified = false;

	private record VerifyCredentialRequest(string Email);
	private record VerifyCredentialResponse(int UserId);
	private record ResetPasswordRequest(string NewPassword);

	public async Task<VerificationResult> VerifyCredential(
		Credentials<User> credentials,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		try
		{
			VerifyCredentialResponse response = await client.GetAsync<VerifyCredentialResponse, VerifyCredentialRequest>(
				apiMethod: AccountControllerMethods.GetEmailOwner,
				argQuery: new VerifyCredentialRequest(Email: credentials.GetCredential<string>(name: nameof(VerifyCredentialRequest.Email))),
				cancellationToken: cancellationToken
			) ?? throw new InvalidOperationException();
			_userId = response.UserId;
			return new VerificationResult(isSuccess: true, errorMessage: String.Empty);
		} catch (Exception ex)
		{
			return new VerificationResult(isSuccess: false, errorMessage: ex.Message);
		}
	}

	public async Task<bool> VerifyAuthenticationCode(
		string code,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		if (Int32.IsNegative(value: _userId))
			throw new InvalidOperationException(message: "Сначала нужно проверить данные для восстановления доступа.");

		_googleAuthenticatorIsVerified = await googleAuthenticatorService.VerifyAuthenticationCode(
			userId: _userId,
			code: code,
			cancellationToken: cancellationToken
		);
		return _googleAuthenticatorIsVerified;
	}

	public async Task ResetPassword(
		string newPassword,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		if (!_googleAuthenticatorIsVerified)
			throw new InvalidOperationException(message: "Сначала необходимо проверить аутентификационный код.");

		await client.PostAsync<ResetPasswordRequest>(
			apiMethod: AccountControllerMethods.ResetPassword(userId: _userId),
			arg: new ResetPasswordRequest(NewPassword: newPassword),
			cancellationToken: cancellationToken
		);
	}
}

public static class RestoringAccessThroughEmailServiceExtension
{
	public static IServiceCollection AddRestoringAccessThroughEmailService(this IServiceCollection serviceCollection)
		=> serviceCollection.AddScoped<IRestoringAccessService<User>, RestoringAccessThroughEmailService>();

	public static IServiceCollection AddKeyedRestoringAccessThroughEmailService(this IServiceCollection serviceCollection, string key)
		=> serviceCollection.AddKeyedScoped<IRestoringAccessService<User>, RestoringAccessThroughEmailService>(serviceKey: key);
}