using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;

namespace MyJournal.Core.UserData;

public sealed class Email(
	ApiClient client,
	IGoogleAuthenticatorService googleAuthenticatorService,
	string? email
)
{
	#region Properies
	public string? Address { get; private set; } = email;
	#endregion

	#region Records
	private sealed record ChangeEmailRequest(string NewEmail);
	private sealed record ChangeEmailResponse(string Email, string Message);
	#endregion

	#region Classes

	public sealed class UpdatedEmailEventArgs(string? email) : EventArgs
	{
		public string? Email { get; } = email;
	}
	#endregion

	#region Delegates
	public delegate void UpdatedEmailHandler(UpdatedEmailEventArgs e);
	#endregion

	#region Events
	public event UpdatedEmailHandler? Updated;
	#endregion

	#region Methods
	public async Task<string> Change(
		string confirmationCode,
		string newEmail,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		bool isVerified = await googleAuthenticatorService.VerifyAuthenticationCode(userId: client.ClientId, code: confirmationCode, cancellationToken: cancellationToken);
		if (!isVerified)
			throw new ArgumentException(message: "Некорректный код подтверждения для смены адреса электронной почты.", paramName: nameof(confirmationCode));

		ChangeEmailResponse response = await client.PutAsync<ChangeEmailResponse, ChangeEmailRequest>(
			apiMethod: UserControllerMethods.ChangeEmail,
			arg: new ChangeEmailRequest(NewEmail: newEmail),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		Address = response.Email;
		return response.Message;
	}

	public override string? ToString()
		=> Address;

	internal void OnUpdated(UpdatedEmailEventArgs e)
	{
		Address = e.Email;
		Updated?.Invoke(e: e);
	}
	#endregion
}