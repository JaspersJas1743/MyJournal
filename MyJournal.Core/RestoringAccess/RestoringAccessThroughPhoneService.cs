using MyJournal.Core.Utilities;

namespace MyJournal.Core.RestoringAccess;

public class RestoringAccessThroughPhoneService(ApiClient client) : IRestoringAccessService<User>
{
	private int _userId = -1;
	private bool _googleAuthenticatorIsVerified = false;

	private record VerifyCredentialRequest(string Phone);
	private record VerifyCredentialResponse(int UserId);
	private record VerifyAuthenticationCodeRequest(string UserCode);
	private record VerifyAuthenticationCodeResponse(bool IsVerified);
	private record ResetPasswordRequest(string NewPassword);

	public async Task<bool> VerifyCredential(
		Credentials<User> credentials,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		try
		{
			string phone = credentials.GetCredential<string>(name: nameof(VerifyCredentialRequest.Phone));
			phone = phone.Replace("+", "%2B").Replace("(", "%28").Replace(")", "%29");
			VerifyCredentialResponse response = await client.GetAsync<VerifyCredentialResponse, VerifyCredentialRequest>(
				apiMethod: "account/restoring-access/phone/user/id/get",
				argQuery: new VerifyCredentialRequest(Phone: phone),
				cancellationToken: cancellationToken
			) ?? throw new InvalidOperationException();
			_userId = response.UserId;
			return true;
		} catch (Exception ex)
		{
			return false;
		}
	}

	public async Task<bool> VerifyAuthenticationCode(
		string code,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		if (Int32.IsNegative(value: _userId))
			throw new InvalidOperationException(message: "Сначала нужно проверить данные для восстановления доступа.");

		VerifyAuthenticationCodeResponse data = await client.GetAsync<VerifyAuthenticationCodeResponse, VerifyAuthenticationCodeRequest>(
			apiMethod: $"account/user/{_userId}/code/verify",
			argQuery: new VerifyAuthenticationCodeRequest(UserCode: code),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		_googleAuthenticatorIsVerified = data.IsVerified;
		return data.IsVerified;
	}

	public async Task ResetPassword(
		string newPassword,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		if (!_googleAuthenticatorIsVerified)
			throw new InvalidOperationException(message: "Сначала необходимо проверить аутентификационный код.");

		await client.PostAsync<ResetPasswordRequest>(
			apiMethod: $"account/restoring-access/user/{_userId}/password/reset",
			arg: new ResetPasswordRequest(NewPassword: newPassword),
			cancellationToken: cancellationToken
		);
	}
}