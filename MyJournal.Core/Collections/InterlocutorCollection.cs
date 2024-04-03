using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.EventArgs;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.Collections;

public sealed class InterlocutorCollection : LazyCollection<Interlocutor>
{
	#region Fields
	private readonly IFileService _fileService;
	#endregion

	#region Constructors
	private InterlocutorCollection(
		ApiClient client,
		IFileService fileService,
		AsyncLazy<List<Interlocutor>> interlocutors,
		int count,
		int offset
	) : base(client: client, collection: interlocutors, count: count, offset: offset)
	{
		_fileService = fileService;
	}
	#endregion

	#region Records
	private sealed record GetInterlocutorsRequest(int Offset, int Count);
	private sealed record GetInterlocutorsResponse(int UserId);
	#endregion

	#region Events
	public event InterlocutorAppearedOnlineHandler? InterlocutorAppearedOnline;
	public event InterlocutorAppearedOfflineHandler? InterlocutorAppearedOffline;
	public event InterlocutorUpdatedPhotoHandler? InterlocutorUpdatedPhoto;
	public event InterlocutorDeletedPhotoHandler? InterlocutorDeletedPhoto;
	#endregion

	#region Methods
	#region Static
	internal static async Task<InterlocutorCollection> Create(
		ApiClient client,
		IFileService fileService,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		const int basedOffset = 0;
		const int basedCount = 20;
		IEnumerable<GetInterlocutorsResponse> interlocutors = await client.GetAsync<IEnumerable<GetInterlocutorsResponse>, GetInterlocutorsRequest>(
			apiMethod: ChatControllerMethods.GetInterlocutors,
			argQuery: new GetInterlocutorsRequest(
				Offset: basedOffset,
				Count: basedCount
			), cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return new InterlocutorCollection(
			client: client,
			fileService: fileService,
			interlocutors: new AsyncLazy<List<Interlocutor>>(valueFactory: async () => new List<Interlocutor>(collection: await Task.WhenAll(
				tasks: interlocutors.Select(selector: async i => await Interlocutor.Create(
					client: client,
					fileService: fileService,
					id: i.UserId,
					cancellationToken:
					cancellationToken
				))
			))),
			count: basedCount,
			offset: interlocutors.Count()
		);
	}
	#endregion

	#region LazyCollection<Interlocutor>
	protected override async Task Load(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<GetInterlocutorsResponse> interlocutors = await Client.GetAsync<IEnumerable<GetInterlocutorsResponse>, GetInterlocutorsRequest>(
			apiMethod: ChatControllerMethods.GetInterlocutors,
			argQuery: new GetInterlocutorsRequest(
				Offset: Offset,
				Count: Count
			), cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		List<Interlocutor> collection = await Collection;
		collection.AddRange(collection: await Task.WhenAll(tasks: interlocutors.Select(selector: async i => await Interlocutor.Create(
			client: Client,
			fileService: _fileService,
			id: i.UserId,
			cancellationToken: cancellationToken
		))));
		Offset = collection.Count;
	}

	internal override async Task Append(
		int id,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await base.Append(instance: await Interlocutor.Create(
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
		await base.Insert(index: index, instance: await Interlocutor.Create(
			client: Client,
			fileService: _fileService,
			id: id,
			cancellationToken: cancellationToken
		), cancellationToken: cancellationToken);
	}
	#endregion

	#region Instance
	internal async Task OnAppearedOnline(InterlocutorAppearedOnlineEventArgs e)
	{
		await InvokeIfInterlocutorIsCreated(
			invocation: async interlocutor => interlocutor.OnAppearedOnline(e: e),
			interlocutorId: e.InterlocutorId
		);

		InterlocutorAppearedOnline?.Invoke(e: e);
	}

	internal async Task OnAppearedOffline(InterlocutorAppearedOfflineEventArgs e)
	{
		await InvokeIfInterlocutorIsCreated(
			invocation: async interlocutor => interlocutor.OnAppearedOffline(e: e),
			interlocutorId: e.InterlocutorId
		);
		InterlocutorAppearedOffline?.Invoke(e: e);
	}

	internal async Task OnUpdatedPhoto(InterlocutorUpdatedPhotoEventArgs e)
	{
		await InvokeIfInterlocutorIsCreated(
			invocation: async interlocutor => await interlocutor.OnUpdatedPhoto(e: e),
			interlocutorId: e.InterlocutorId
		);
		InterlocutorUpdatedPhoto?.Invoke(e: e);
	}

	internal async Task OnDeletedPhoto(InterlocutorDeletedPhotoEventArgs e)
	{
		await InvokeIfInterlocutorIsCreated(
			invocation: async interlocutor => await interlocutor.OnDeletedPhoto(e: e),
			interlocutorId: e.InterlocutorId
		);
		InterlocutorDeletedPhoto?.Invoke(e: e);
	}

	private async Task InvokeIfInterlocutorIsCreated(
		Func<Interlocutor, Task> invocation,
		int interlocutorId
	)
	{
		if (!Collection.IsValueCreated)
			return;

		Interlocutor interlocutor = await FindById(id: interlocutorId);
		await invocation(arg: interlocutor);
	}
	#endregion
	#endregion
}