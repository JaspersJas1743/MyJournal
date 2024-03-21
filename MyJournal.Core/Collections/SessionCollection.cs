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
		IEnumerable<Session.GetSessionsResponse> sessions = await client.GetAsync<IEnumerable<Session.GetSessionsResponse>>(
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

	internal async Task Clear(CancellationToken cancellationToken = default(CancellationToken))
		=> _sessions.Value.Clear();

	internal async Task Append(
		int id,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		_sessions.Value.Add(item: await Session.Create(
			client: _client,
			id: id,
			cancellationToken: cancellationToken
		));
	}

	internal async Task Insert(
		int index,
		int id,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		_sessions.Value.Insert(index: index, item: await Session.Create(
			client: _client,
			id: id,
			cancellationToken: cancellationToken
		));
	}

	internal async Task RemoveRange(
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