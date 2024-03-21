using System.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.Collections;

public sealed class SessionCollection : IEnumerable<Session>
{
	#region Fields
	private readonly ApiClient _client;
	private readonly Lazy<List<Session>> _sessions = new Lazy<List<Session>>(value: new List<Session>());
	#endregion

	#region Constructor
	private SessionCollection(
		ApiClient client,
		IEnumerable<Session> sessions
	)
	{
		_client = client;
		_sessions.Value.AddRange(collection: sessions);
	}
	#endregion

	#region Properties
	public Session this[int id]
		=> _sessions.Value.Find(match: i => i.Id.Equals(id))
		   ?? throw new ArgumentOutOfRangeException(message: $"Сессия с идентификатором {id} отсутствует или не загружен.", paramName: nameof(id));
	#endregion

	#region Records
	private record GetSessionsResponse(int Id, string ClientName, string ClientLogoLink, string Ip, bool IsCurrentSession);
	private sealed record SignOutResponse(string Message);
	#endregion

	#region Classes

	public sealed class CreatedSessionEventArgs(int sessionId) : EventArgs
	{
		public int SessionId { get; } = sessionId;
	}

	public sealed class ClosedSessionEventArgs(IEnumerable<int> sessionIds, bool currentSessionAreClosed) : EventArgs
	{
		public IEnumerable<int> SessionIds { get; } = sessionIds;
		public bool CurrentSessionAreClosed { get; } = currentSessionAreClosed;
	}
	#endregion

	#region Delegated
	public delegate void CreatedSessionHandler(CreatedSessionEventArgs e);
	public delegate void ClosedSessionHandler(ClosedSessionEventArgs e);
	#endregion

	#region Events
	public event CreatedSessionHandler? CreatedSession;
	public event ClosedSessionHandler? ClosedSession;
	#endregion

	#region Methods
	#region Static
	internal static async Task<SessionCollection> Create(
		ApiClient client,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<GetSessionsResponse> sessions = await client.GetAsync<IEnumerable<GetSessionsResponse>>(
			apiMethod: AccountControllerMethods.GetSessions,
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return new SessionCollection(
			client: client,
			sessions: sessions.Select(selector: s =>
				Session.Create(
					client: client,
					id: s.Id,
					cancellationToken: cancellationToken
				).GetAwaiter().GetResult()
			)
		);
	}
	#endregion

	#region Instance
	private async Task<string> SignOut(
		string method,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		SignOutResponse response = await _client.PostAsync<SignOutResponse>(
			apiMethod: method,
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return response.Message;
	}

	public async Task Clear(CancellationToken cancellationToken = default(CancellationToken))
		=> _sessions.Value.Clear();

	public async Task Append(
		int id,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		Session.GetSessionsResponse session = await _client.GetAsync<Session.GetSessionsResponse>(
			apiMethod: AccountControllerMethods.GetSession(sessionId: id),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		_sessions.Value.Add(item: await Session.Create(
			client: _client,
			id: session.Id,
			cancellationToken: cancellationToken
		));
	}

	public async Task Remove(
		int id,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		Session? session = _sessions.Value.Find(match: s => s.Id == id);
		if (session is null)
			throw new ArgumentOutOfRangeException(message: $"Сессия с идентификатором {id} не найдена.", paramName: nameof(id));

		_sessions.Value.Remove(item: session);
	}

	public async Task RemoveRange(
		IEnumerable<int> ids,
		CancellationToken cancellationToken = default(CancellationToken)
	) => _sessions.Value.RemoveAll(match: s => ids.Contains(value: s.Id));

	public async Task<string> CloseThis(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		string message = await SignOut(method: AccountControllerMethods.SignOutThis, cancellationToken: cancellationToken);
		_client.Token = null;
		return message;
	}

	public async Task<string> CloseAll(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		string message = await SignOut(method: AccountControllerMethods.SignOutAll, cancellationToken: cancellationToken);
		_client.Token = null;
		return message;
	}

	public async Task<string> CloseOthers(
		CancellationToken cancellationToken = default(CancellationToken)
	) => await SignOut(method: AccountControllerMethods.SignOutOthers, cancellationToken: cancellationToken);

	internal async Task OnCreatedSession(
		CreatedSessionEventArgs e,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await Insert(index: 0, id: e.SessionId, cancellationToken: cancellationToken);
		this[id: e.SessionId].OnCreated(e: new Session.CreatedEventArgs());
		CreatedSession?.Invoke(e: e);
	}

	internal async Task OnClosedSession(
		ClosedSessionEventArgs e,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		foreach (int sessionId in e.SessionIds)
			this[id: sessionId].OnClosed(e: new Session.ClosedEventArgs());
		await RemoveRange(ids: e.SessionIds, cancellationToken: cancellationToken);
		ClosedSession?.Invoke(e: e);
	}
	#endregion

	#region IEnumerable<Session>
	public IEnumerator<Session> GetEnumerator()
		=> _sessions.Value.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() =>
		GetEnumerator();
	#endregion
	#endregion
}