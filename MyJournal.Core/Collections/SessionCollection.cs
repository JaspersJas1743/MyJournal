using System.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.EventArgs;

namespace MyJournal.Core.Collections;

public sealed class SessionCollection : IAsyncEnumerable<Session>
{
	#region Fields
	private readonly ApiClient _client;
	private readonly AsyncLazy<List<Session>> _sessions;
	#endregion

	#region Constructor
	private SessionCollection(
		ApiClient client,
		AsyncLazy<List<Session>> sessions
	)
	{
		_client = client;
		_sessions = sessions;
	}
	#endregion

	#region Records
	private sealed record SignOutResponse(string Message);
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
			sessions: new AsyncLazy<List<Session>>(valueFactory: async () => new List<Session>(collection: await Task.WhenAll(tasks: sessions.Select(
				selector: async s => await Session.Create(client: client, id: s.Id, cancellationToken: cancellationToken)
			)))));
	}
	#endregion

	#region Instance
	public async Task<Session> FindById(int id)
	{
		List<Session> sessions = await _sessions;
		return sessions.Find(match: i => i.Id.Equals(id)) ?? throw new ArgumentOutOfRangeException(
			message: $"Сессия с идентификатором {id} отсутствует или не загружен.", paramName: nameof(id)
		);
	}

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

	internal async Task Clear(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		List<Session> sessions = await _sessions;
		sessions.Clear();
	}

	internal async Task Append(
		int id,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		List<Session> sessions = await _sessions;
		sessions.Add(item: await Session.Create(
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
		List<Session> sessions = await _sessions;
		sessions.Insert(index: index, item: await Session.Create(
			client: _client,
			id: id,
			cancellationToken: cancellationToken
		));
	}

	internal async Task RemoveRange(
		IEnumerable<int> ids,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		List<Session> sessions = await _sessions;
		sessions.RemoveAll(match: s => ids.Contains(value: s.Id));
	}

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
		if (!_sessions.IsValueCreated)
			return;

		await Insert(index: 0, id: e.SessionId, cancellationToken: cancellationToken);
		Session session = await FindById(id: e.SessionId);
		session.OnCreated(e: e);
		CreatedSession?.Invoke(e: e);
	}

	internal async Task OnClosedSession(
		ClosedSessionEventArgs e,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		if (!_sessions.IsValueCreated)
			return;

		foreach (int sessionId in e.SessionIds)
		{
			Session session = await FindById(id: sessionId);
			session.OnClosed(e: e);
		}
		await RemoveRange(ids: e.SessionIds, cancellationToken: cancellationToken);
		ClosedSession?.Invoke(e: e);
	}
	#endregion

	#region IAsyncEnumerable<Session>
	public async IAsyncEnumerator<Session> GetAsyncEnumerator(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		foreach (Session session in await _sessions)
		{
			if (cancellationToken.IsCancellationRequested)
				yield break;

			yield return session;
		}
	}
	#endregion
	#endregion
}