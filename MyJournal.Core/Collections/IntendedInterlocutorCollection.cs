using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.Collections;

public sealed class IntendedInterlocutorCollection : LazyCollection<IntendedInterlocutor>
{
	#region Fields
	private readonly IFileService _fileService;
	#endregion

	#region Constructors
	private IntendedInterlocutorCollection(
		ApiClient client,
		IFileService fileService,
		IEnumerable<IntendedInterlocutor> intendedInterlocutors,
		int count,
		bool includeExistedInterlocutors
	) : base(client: client, collection: intendedInterlocutors, count: count)
	{
		_fileService = fileService;
		IncludeExistedInterlocutors = includeExistedInterlocutors;
	}
	#endregion

	#region Properties
	public bool IncludeExistedInterlocutors { get; private set; }
	public string? Filter { get; private set; } = String.Empty;
	#endregion

	#region Records
	private sealed record GetInterlocutorsRequest(bool IncludeExistedInterlocutors, bool IsFiltered, string? Filter, int Offset, int Count);
	private sealed record GetInterlocutorsResponse(int UserId);
	#endregion

	#region Methods
	#region Static
	internal static async Task<IntendedInterlocutorCollection> Create(
		ApiClient client,
		IFileService fileService,
		bool includeExistedInterlocutors = true,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		const int basedOffset = 0;
		const int basedCount = 20;
		IEnumerable<GetInterlocutorsResponse> interlocutors = await client.GetAsync<IEnumerable<GetInterlocutorsResponse>, GetInterlocutorsRequest>(
			apiMethod: ChatControllerMethods.GetIntendedInterlocutors,
			argQuery: new GetInterlocutorsRequest(
				IsFiltered: false,
				Filter: String.Empty,
				Offset: basedOffset,
				Count: basedCount,
				IncludeExistedInterlocutors: includeExistedInterlocutors
			), cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return new IntendedInterlocutorCollection(
			client: client,
			fileService: fileService,
			intendedInterlocutors: interlocutors.Select(selector: i =>
				IntendedInterlocutor.Create(
					client: client,
					fileService: fileService,
					id: i.UserId,
					cancellationToken: cancellationToken
				).GetAwaiter().GetResult()
			),
			count: basedCount,
			includeExistedInterlocutors: includeExistedInterlocutors
		);
	}
	#endregion

	#region Instance
	private async Task LoadIntendedInterlocutors(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<GetInterlocutorsResponse> interlocutors = await _client.GetAsync<IEnumerable<GetInterlocutorsResponse>, GetInterlocutorsRequest>(
			apiMethod: ChatControllerMethods.GetInterlocutors,
			argQuery: new GetInterlocutorsRequest(
				IsFiltered: !String.IsNullOrWhiteSpace(value: Filter),
				Filter: Filter,
				Offset: _offset,
				Count: _count,
				IncludeExistedInterlocutors: IncludeExistedInterlocutors
			), cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		_collection.AddRange(collection: interlocutors.Select(selector: i =>
			IntendedInterlocutor.Create(
				client: _client,
				fileService: _fileService,
				id: i.UserId,
				cancellationToken: cancellationToken
			).GetAwaiter().GetResult()
		));
		_offset = _collection.Count;
	}

	public override async Task LoadNext(
		CancellationToken cancellationToken = default(CancellationToken)
	) => await LoadIntendedInterlocutors(cancellationToken: cancellationToken);

	public override async Task Clear(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		_collection.Clear();
		_offset = _collection.Count;
		Filter = String.Empty;
		IncludeExistedInterlocutors = false;
	}

	public override async Task Append(
		int id,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IntendedInterlocutor intendedInterlocutor = await _client.GetAsync<IntendedInterlocutor>(
			apiMethod: UserControllerMethods.GetInformationAbout(userId: id),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		_collection.Insert(index: 0, item: intendedInterlocutor);
		_offset = _collection.Count;
	}

	public async Task SetFilter(
		string? filter,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await Clear(cancellationToken: cancellationToken);
		Filter = filter;
		await LoadIntendedInterlocutors(cancellationToken: cancellationToken);
	}

	public async Task SetIncludeExistedInterlocutors(
		bool includeExistedInterlocutors,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await Clear(cancellationToken: cancellationToken);
		IncludeExistedInterlocutors = includeExistedInterlocutors;
		await LoadIntendedInterlocutors(cancellationToken: cancellationToken);
	}
	#endregion
	#endregion
}