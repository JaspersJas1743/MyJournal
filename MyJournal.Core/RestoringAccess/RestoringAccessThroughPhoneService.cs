using Microsoft.Extensions.DependencyInjection;
using MyJournal.Core.Utilities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;

namespace MyJournal.Core.RestoringAccess;

public class RestoringAccessThroughPhoneService(
	ApiClient client,
	IGoogleAuthenticatorService googleAuthenticatorService
) : IRestoringAccessService<User>
{
	private int _userId = -1;
	private bool _googleAuthenticatorIsVerified = false;

	private record VerifyCredentialRequest(string Phone);
	private record VerifyCredentialResponse(int UserId);
	private record ResetPasswordRequest(string NewPassword);

	public async Task<VerificationResult> VerifyCredential(
		Credentials<User> credentials,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		try
		{
			string phone = credentials.GetCredential<string>(name: nameof(VerifyCredentialRequest.Phone));
			phone = phone.Replace(oldValue: "+", newValue: "%2B").Replace(oldValue: "(", newValue: "%28").Replace(oldValue: ")", newValue: "%29");
			VerifyCredentialResponse response = await client.GetAsync<VerifyCredentialResponse, VerifyCredentialRequest>(
				apiMethod: AccountControllerMethods.GetPhoneOwner,
				argQuery: new VerifyCredentialRequest(Phone: phone),
				cancellationToken: cancellationToken
			) ?? throw new InvalidOperationException();
			_userId = response.UserId;
			return new VerificationResult(isSuccess: true, errorMessage: String.Empty);
		} catch (ApiException ex)
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

public static class RestoringAccessThroughPhoneServiceExtension
{
	public static IServiceCollection AddRestoringAccessThroughPhoneService(this IServiceCollection serviceCollection)
		=> serviceCollection.AddScoped<IRestoringAccessService<User>, RestoringAccessThroughPhoneService>();

	public static IServiceCollection AddKeyedRestoringAccessThroughPhoneService(this IServiceCollection serviceCollection, string key)
		=> serviceCollection.AddKeyedScoped<IRestoringAccessService<User>, RestoringAccessThroughPhoneService>(serviceKey: key);
}