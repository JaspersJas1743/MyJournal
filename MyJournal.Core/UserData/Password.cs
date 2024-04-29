using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;

namespace MyJournal.Core.UserData;

public sealed class Password(
	ApiClient client,
	IGoogleAuthenticatorService googleAuthenticatorService
)
{
	#region Records
	private sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
	private sealed record ChangePasswordResponse(string Message);
	#endregion

	#region Methods
	public async Task<string> Change(
		string confirmationCode,
		string currentPassword,
		string newPassword,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		bool isVerified = await googleAuthenticatorService.VerifyAuthenticationCode(userId: client.ClientId, code: confirmationCode, cancellationToken: cancellationToken);
		if (!isVerified)
			throw new ArgumentException(message: "Некорректный код подтверждения для смены пароля.");

		ChangePasswordResponse response = await client.PutAsync<ChangePasswordResponse, ChangePasswordRequest>(
			apiMethod: UserControllerMethods.ChangePassword,
			arg: new ChangePasswordRequest(CurrentPassword: currentPassword, NewPassword: newPassword),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return response.Message;
	}
	#endregion
}