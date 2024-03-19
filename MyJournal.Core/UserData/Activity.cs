using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.UserData;

public sealed class Activity(
	ApiClient client
)
{
	public enum Statuses
	{
		Online,
		Offline
	}

	private async Task SetActivityStatus(
		string method,
		CancellationToken cancellationToken = default(CancellationToken)
	) => await client.PutAsync(apiMethod: method, cancellationToken: cancellationToken);

	public async Task SetOffline(
		CancellationToken cancellationToken = default(CancellationToken)
	) => await SetActivityStatus(method: UserControllerMethods.SetOffline, cancellationToken: cancellationToken);

	public async Task SetOnline(
		CancellationToken cancellationToken = default(CancellationToken)
	) => await SetActivityStatus(method: UserControllerMethods.SetOnline, cancellationToken: cancellationToken);
}