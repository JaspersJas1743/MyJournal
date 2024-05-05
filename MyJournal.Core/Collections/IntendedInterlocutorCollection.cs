using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.FileService;
using Activity = MyJournal.Core.UserData.Activity;

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
		AsyncLazy<List<IntendedInterlocutor>> intendedInterlocutors,
		int count,
		int offset,
		bool includeExistedInterlocutors
	) : base(client: client, collection: intendedInterlocutors, count: count, offset: offset)
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
	internal sealed record GetInterlocutorsResponse(int Id, string Surname, string Name, string? Patronymic, string? Photo, Activity.Statuses Activity, DateTime? OnlineAt);
	#endregion

	#region Methods
	#region Static
	internal static async Task<IntendedInterlocutorCollection> Create(
		ApiClient client,
		IFileService fileService,
		bool includeExistedInterlocutors = false,
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
			intendedInterlocutors: new AsyncLazy<List<IntendedInterlocutor>>(valueFactory: () => new List<IntendedInterlocutor>(
				collection: interlocutors.Select(selector: i => IntendedInterlocutor.Create(
					client: client,
					fileService: fileService,
					response: i
				))
			)),
			count: basedCount,
			offset: interlocutors.Count(),
			includeExistedInterlocutors: includeExistedInterlocutors
		);
	}
	#endregion

	#region LazyCollection<IntendedInterlocutor>
	protected override async Task Load(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<GetInterlocutorsResponse> interlocutors = await Client.GetAsync<IEnumerable<GetInterlocutorsResponse>, GetInterlocutorsRequest>(
			apiMethod: ChatControllerMethods.GetIntendedInterlocutors,
			argQuery: new GetInterlocutorsRequest(
				IsFiltered: !String.IsNullOrWhiteSpace(value: Filter),
				Filter: Filter,
				Offset: Offset,
				Count: Count,
				IncludeExistedInterlocutors: IncludeExistedInterlocutors
			), cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		List<IntendedInterlocutor> collection = await Collection;
		collection.AddRange(collection: interlocutors.Select(
			selector: i => IntendedInterlocutor.Create(
				client: Client,
				fileService: _fileService,
				response: i
			)
		));
		Offset = collection.Count;
	}

	internal override async Task Append(
		int id,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await base.Append(instance: await IntendedInterlocutor.Create(
			client: Client,
			fileService: _fileService,
			id: id,
			cancellationToken: cancellationToken
		), cancellationToken: cancellationToken);
	}

	internal override async Task Insert(
		int index,
		int id,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await base.Insert(index: index, instance: await IntendedInterlocutor.Create(
			client: Client,
			fileService: _fileService,
			id: id,
			cancellationToken: cancellationToken
		), cancellationToken: cancellationToken);
	}

	internal async Task Remove(
		int interlocutorId,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		List<IntendedInterlocutor> collection = await Collection;
		collection.RemoveAll(match: interlocutor => interlocutor.Id == interlocutorId);
	}
	#endregion

	#region Instance
	public async Task SetFilter(
		string? filter,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await Clear(cancellationToken: cancellationToken);
		Filter = filter;
		await Load(cancellationToken: cancellationToken);
	}

	public async Task SetIncludeExistedInterlocutors(
		bool includeExistedInterlocutors,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await Clear(cancellationToken: cancellationToken);
		IncludeExistedInterlocutors = includeExistedInterlocutors;
		await Load(cancellationToken: cancellationToken);
	}
	#endregion
	#endregion
}