using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.EventArgs;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;

namespace MyJournal.Core.UserData;

public sealed class Phone(
	ApiClient client,
	IGoogleAuthenticatorService googleAuthenticatorService,
	string? phone
)
{
	#region Properties
	public string? Number { get; private set; } = phone;
	#endregion

	#region Records
	private sealed record ChangePhoneRequest(string NewPhone);
	private sealed record ChangePhoneResponse(string Phone, string Message);
	#endregion

	#region Events
	public event UpdatedPhoneHandler? Updated;
	#endregion

	#region Methods
	public async Task<string> Change(
		string confirmationCode,
		string newPhone,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		bool isVerified = await googleAuthenticatorService.VerifyAuthenticationCode(userId: client.ClientId, code: confirmationCode, cancellationToken: cancellationToken);
		if (!isVerified)
			throw new ArgumentException(message: "Некорректный код подтверждения для смены номера телефона.");

		ChangePhoneResponse response = await client.PutAsync<ChangePhoneResponse, ChangePhoneRequest>(
			apiMethod: UserControllerMethods.ChangePhone,
			arg: new ChangePhoneRequest(NewPhone: newPhone),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		Number = response.Phone;
		return response.Message;
	}

	public override string? ToString()
		=> Number;

	internal void OnUpdated(UpdatedPhoneEventArgs e)
	{
		Number = e.Phone;
		Updated?.Invoke(e: e);
	}
	#endregion
}