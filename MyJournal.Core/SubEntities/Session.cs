using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.EventArgs;

namespace MyJournal.Core.SubEntities;

public sealed class Session : ISubEntity
{
	#region Fields
	private readonly ApiClient _client;
	#endregion

	#region Constructors
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
	#endregion

	#region Properties
	public int Id { get; init; }
	public string ClientName { get; init; }
	public string ClientLogoLink { get; init; }
	public string Ip { get; init; }
	public bool IsCurrentSession { get; init; }
	#endregion

	#region Records
	internal sealed record GetSessionsResponse(int Id, string ClientName, string ClientLogoLink, string Ip, bool IsCurrentSession);
	private sealed record SignOutResponse(string Message);
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
		GetSessionsResponse response = await client.GetAsync<GetSessionsResponse>(
			apiMethod: AccountControllerMethods.GetSession(sessionId: id),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return new Session(client: client, response: response);
	}

	internal static Session Create(
		ApiClient client,
		GetSessionsResponse response
	) => new Session(client: client, response: response);

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

	internal void OnCreated(CreatedSessionEventArgs e)
		=> Created?.Invoke(e: e);

	internal void OnClosed(ClosedSessionEventArgs e)
		=> Closed?.Invoke(e: e);
	#endregion
}