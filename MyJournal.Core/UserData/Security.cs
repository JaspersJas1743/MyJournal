using MyJournal.Core.Utilities;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.UserData;

public sealed class Security(
	ApiClient client
)
{
	#region Properties
	public Phone? Phone { get; init; }
	public Email? Email { get; init; }
	public Password? Password { get; init; }
	#endregion

	#region Records
	private sealed record SignOutResponse(string Message);
	#endregion

	#region Methods
	private async Task<string> SignOut(
		string method,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		SignOutResponse response = await client.PostAsync<SignOutResponse>(
			apiMethod: method,
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return response.Message;
	}

	public async Task<string> SignOutThisSession(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		string message = await SignOut(method: AccountControllerMethods.SignOutThis, cancellationToken: cancellationToken);
		client.Token = null;
		return message;
	}

	public async Task<string> SignOutAllSessions(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		string message = await SignOut(method: AccountControllerMethods.SignOutAll, cancellationToken: cancellationToken);
		client.Token = null;
		return message;
	}

	public async Task<string> SignOutOthersSessions(
		CancellationToken cancellationToken = default(CancellationToken)
	) => await SignOut(method: AccountControllerMethods.SignOutOthers, cancellationToken: cancellationToken);
	#endregion
}