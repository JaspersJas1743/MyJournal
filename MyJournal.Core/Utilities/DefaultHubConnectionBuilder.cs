using System.Net;
using Microsoft.AspNetCore.SignalR.Client;

namespace MyJournal.Core.Utilities;

public static class DefaultHubConnectionBuilder
{
	public static HubConnection CreateHubConnection(string url, string token)
	{
		return new HubConnectionBuilder().WithUrl(url: url, configureHttpConnection:
			options => options.Headers.Add(key: nameof(HttpRequestHeader.Authorization), value: token)
		).WithAutomaticReconnect().Build();
	}
}