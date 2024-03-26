using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;
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
		IEnumerable<Interlocutor> interlocutors,
		int count
	) : base(client: client, collection: interlocutors, count: count)
	{
		_fileService = fileService;
	}
	#endregion

	#region Records
	private sealed record GetInterlocutorsRequest(int Offset, int Count);
	private sealed record GetInterlocutorsResponse(int UserId);
	#endregion

	#region Classes
	public sealed class InterlocutorAppearedOnlineEventArgs(int interlocutorId, DateTime? onlineAt) : EventArgs
	{
		public int InterlocutorId { get; } = interlocutorId;
		public DateTime? OnlineAt { get; } = onlineAt;
	}

	public sealed class InterlocutorAppearedOfflineEventArgs(int interlocutorId, DateTime? onlineAt) : EventArgs
	{
		public int InterlocutorId { get; } = interlocutorId;
		public DateTime? OnlineAt { get; } = onlineAt;
	}

	public sealed class InterlocutorUpdatedPhotoEventArgs(int interlocutorId, string link) : EventArgs
	{
		public int InterlocutorId { get; } = interlocutorId;
		public string Link { get; } = link;
	}

	public sealed class InterlocutorDeletedPhotoEventArgs(int interlocutorId) : EventArgs
	{
		public int InterlocutorId { get; } = interlocutorId;
	}
	#endregion

	#region Delegates
	public delegate void InterlocutorAppearedOnlineHandler(InterlocutorAppearedOnlineEventArgs e);
	public delegate void InterlocutorAppearedOfflineHandler(InterlocutorAppearedOfflineEventArgs e);
	public delegate void InterlocutorUpdatedPhotoHandler(InterlocutorUpdatedPhotoEventArgs e);
	public delegate void InterlocutorDeletedPhotoHandler(InterlocutorDeletedPhotoEventArgs e);
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
			interlocutors: interlocutors.Select(selector: i =>
				Interlocutor.Create(
					client: client,
					fileService: fileService,
					id: i.UserId,
					cancellationToken: cancellationToken
				).GetAwaiter().GetResult()
			),
			count: basedCount
		);
	}
	#endregion

	#region LazyCollection<Interlocutor>
	protected override async Task Load(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<GetInterlocutorsResponse> interlocutors = await _client.GetAsync<IEnumerable<GetInterlocutorsResponse>, GetInterlocutorsRequest>(
			apiMethod: ChatControllerMethods.GetInterlocutors,
			argQuery: new GetInterlocutorsRequest(
				Offset: _offset,
				Count: _count
			), cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		_collection.Value.AddRange(collection: interlocutors.Select(selector: i =>
			Interlocutor.Create(
				client: _client,
				fileService: _fileService,
				id: i.UserId,
				cancellationToken: cancellationToken
			).GetAwaiter().GetResult()
		));
		_offset = _collection.Value.Count;
	}

	internal override async Task Append(
		int id,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await base.Append(instance: await Interlocutor.Create(
			client: _client,
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
			client: _client,
			fileService: _fileService,
			id: id,
			cancellationToken: cancellationToken
		), cancellationToken: cancellationToken);
	}
	#endregion

	#region Instance
	internal void OnAppearedOnline(InterlocutorAppearedOnlineEventArgs e)
	{
		this[id: e.InterlocutorId].OnAppearedOnline(e: new Interlocutor.AppearedOnlineEventArgs(
			onlineAt: e.OnlineAt
		));
		InterlocutorAppearedOnline?.Invoke(e: e);
	}

	internal void OnAppearedOffline(InterlocutorAppearedOfflineEventArgs e)
	{
		this[id: e.InterlocutorId].OnAppearedOffline(e: new Interlocutor.AppearedOfflineEventArgs(
			onlineAt: e.OnlineAt
		));
		InterlocutorAppearedOffline?.Invoke(e: e);
	}

	internal void OnUpdatedPhoto(InterlocutorUpdatedPhotoEventArgs e)
	{
		this[id: e.InterlocutorId].OnUpdatedPhoto(e: new Interlocutor.UpdatedPhotoEventArgs(link: e.Link));
		InterlocutorUpdatedPhoto?.Invoke(e: e);
	}

	internal void OnDeletedPhoto(InterlocutorDeletedPhotoEventArgs e)
	{
		this[id: e.InterlocutorId].OnDeletedPhoto(e: new Interlocutor.DeletedPhotoEventArgs());
		InterlocutorDeletedPhoto?.Invoke(e: e);
	}
	#endregion
	#endregion
}