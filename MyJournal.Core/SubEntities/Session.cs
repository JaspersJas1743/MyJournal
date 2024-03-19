using System.Diagnostics;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.SubEntities;

public sealed class Session : ISubEntity
{
	private readonly ApiClient _client;

	private Session(
		ApiClient client,
		GetSessionsResponse response
	)
	{
		_client = client;

		Id = response.Id;
		ClientName = response.ClientName;
		ClientLogoLink = response.ClientLogoLink;
		Ip = response.Ip;
		IsCurrentSession = response.IsCurrentSession;
	}

	#region Properties
	public int Id { get; init; }
	public string ClientName { get; init; }
	public string ClientLogoLink { get; init; }
	public string Ip { get; init; }
	public bool IsCurrentSession { get; init; }
	#endregion

	#region Records
	public record GetSessionsResponse(int Id, string ClientName, string ClientLogoLink, string Ip, bool IsCurrentSession);
	private sealed record SignOutResponse(string Message);
	#endregion

	#region Classes
	public sealed class CreatedEventArgs : EventArgs;

	public sealed class ClosedEventArgs : EventArgs;
	#endregion

	#region Delegated
	public delegate void CreatedSessionHandler(CreatedEventArgs e);
	public delegate void ClosedSessionHandler(ClosedEventArgs e);
	#endregion

	#region Events
	public event CreatedSessionHandler? Created;
	public event ClosedSessionHandler? Closed;
	#endregion

	#region Methods
	internal static async Task<Session> Create(
		ApiClient client,
		int id,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		string apiMethod = AccountControllerMethods.GetSession(sessionId: id);
		GetSessionsResponse response = await client.GetAsync<GetSessionsResponse>(
			apiMethod: apiMethod,
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return new Session(client: client, response: response);
	}

	public async Task<string> SignOut(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		SignOutResponse response = await _client.PostAsync<SignOutResponse>(
			apiMethod: AccountControllerMethods.SignOutSession(sessionId: Id),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return response.Message;
	}

	internal void OnCreated(CreatedEventArgs e)
		=> Created?.Invoke(e: e);

	internal void OnClosed(ClosedEventArgs e)
		=> Closed?.Invoke(e: e);
	#endregion
}