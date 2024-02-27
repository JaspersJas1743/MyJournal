using Microsoft.AspNetCore.SignalR.Client;

namespace MyJournal.Core.Utilities;

public static class DefaultHubConnectionBuilder
{
	public static HubConnection CreateHubConnection(string url)
	{
		return new HubConnectionBuilder().WithUrl(url: url, configureHttpConnection:
			options => options.Headers.Add(key: "Authorization", value: ApiClient.Token)
		).Build();
	}
}