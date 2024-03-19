using System.Net;
using Microsoft.AspNetCore.SignalR.Client;

namespace MyJournal.Core.Utilities;

internal static class DefaultHubConnectionBuilder
{
	internal static HubConnection CreateHubConnection(string url, string token)
	{
		return new HubConnectionBuilder().WithUrl(url: url, configureHttpConnection:
			options => options.Headers.Add(key: nameof(HttpRequestHeader.Authorization), value: token)
		).WithAutomaticReconnect().Build();
	}
}