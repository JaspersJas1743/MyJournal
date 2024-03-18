using System.Collections;
using MyJournal.Core.Utilities;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.Interlocutors;

public class InterlocutorCollection : IEnumerable<Interlocutor>
{
	#region Fields
	private readonly ApiClient _client;
	private readonly List<Interlocutor> _interlocutors = new List<Interlocutor>();
	private readonly int _count;

	private int _offset;
	private string _filter = String.Empty;
	private bool _includeExistedInterlocutors = false;
	#endregion

	#region Constructors
	private InterlocutorCollection(ApiClient client, IEnumerable<Interlocutor> interlocutors, int count, bool includeExistedInterlocutors)
	{
		_client = client;
		_interlocutors.AddRange(collection: interlocutors);
		_offset = _interlocutors.Count;
		_count = count;

		_includeExistedInterlocutors = includeExistedInterlocutors;
	}
	#endregion

	#region Properties
	public bool IncludeExistedInterlocutors => _includeExistedInterlocutors;

	public string Filter => _filter;

	public int Length => _interlocutors.Count;

	public Interlocutor this[int index]
		=> _interlocutors.ElementAtOrDefault(index: index)
		?? throw new ArgumentOutOfRangeException(message: $"{index} элемент отсутствует или не загружен.", paramName: nameof(index));
	#endregion

	#region Records
	private sealed record GetInterlocutorsRequest(bool IncludeExistedInterlocutors, bool IsFiltered, string? Filter, int Offset, int Count);

	private sealed record GetInterlocutorsResponse(int UserId, string? Photo, string? Name);
	#endregion

	#region Methods
	#region Static
	public static async Task<InterlocutorCollection> Create(
		ApiClient client,
		bool includeExistedInterlocutors = false,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		const int basedOffset = 0;
		const int basedCount = 20;
		IEnumerable<GetInterlocutorsResponse> interlocutors = await client.GetAsync<IEnumerable<GetInterlocutorsResponse>, GetInterlocutorsRequest>(
			apiMethod: ChatsControllerMethods.GetInterlocutors,
			argQuery: new GetInterlocutorsRequest(
				IsFiltered: false,
				Filter: String.Empty,
				Offset: basedOffset,
				Count: basedCount,
				IncludeExistedInterlocutors: includeExistedInterlocutors
			), cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return new InterlocutorCollection(
			client: client,
			interlocutors: interlocutors.Select(selector: i =>
				Interlocutor.Create(client: client, id: i.UserId, cancellationToken: cancellationToken).GetAwaiter().GetResult()
			),
			count: basedCount,
			includeExistedInterlocutors: includeExistedInterlocutors
		);
	}
	#endregion

	#region Instance
	private async Task LoadInterlocutors(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<GetInterlocutorsResponse> interlocutors = await _client.GetAsync<IEnumerable<GetInterlocutorsResponse>, GetInterlocutorsRequest>(
			apiMethod: ChatsControllerMethods.GetInterlocutors,
			argQuery: new GetInterlocutorsRequest(
				IsFiltered: !String.IsNullOrWhiteSpace(value: _filter),
				Filter: _filter,
				Offset: _offset,
				Count: _count,
				IncludeExistedInterlocutors: IncludeExistedInterlocutors
			), cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		_interlocutors.AddRange(collection: interlocutors.Select(selector: i =>
			Interlocutor.Create(client: _client, id: i.UserId, cancellationToken: cancellationToken).GetAwaiter().GetResult()
		));
		_offset = _interlocutors.Count;
	}

	public async Task LoadNext(
		CancellationToken cancellationToken = default(CancellationToken)
	) => await LoadInterlocutors(cancellationToken: cancellationToken);

	public async Task Clear(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		_interlocutors.Clear();
		_offset = _interlocutors.Count;
		_filter = String.Empty;
		_includeExistedInterlocutors = false;
	}

	public async Task SetFilter(
		string filter,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await Clear(cancellationToken: cancellationToken);
		_filter = filter;
		await LoadInterlocutors(cancellationToken: cancellationToken);
	}

	public async Task SetIncludeExistedInterlocutors(
		bool includeExistedInterlocutors,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await Clear(cancellationToken: cancellationToken);
		_includeExistedInterlocutors = includeExistedInterlocutors;
		await LoadInterlocutors(cancellationToken: cancellationToken);
	}
	#endregion

	#region IEnumerable<Interlocutor>
	public IEnumerator<Interlocutor> GetEnumerator()
		=> _interlocutors.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> GetEnumerator();
	#endregion

	#region Overriden
	public override bool Equals(object? obj)
	{
		if (obj is InterlocutorCollection collection)
			return this.SequenceEqual(second: collection);
		return false;
	}
	#endregion
	#endregion
}