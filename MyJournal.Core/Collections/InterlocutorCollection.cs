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

	public sealed class InterlocutorUpdatedPhotoEventArgs(int interlocutorId) : EventArgs
	{
		public int InterlocutorId { get; } = interlocutorId;
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
	public override async Task LoadNext(
		CancellationToken cancellationToken = default(CancellationToken)
	) => await LoadInterlocutors(cancellationToken: cancellationToken);

	public override async Task Clear(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		_collection.Clear();
		_offset = _collection.Count;
	}

	public override async Task Append(
		int id,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		Interlocutor interlocutor = await _client.GetAsync<Interlocutor>(
			apiMethod: UserControllerMethods.GetInformationAbout(userId: id),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		_collection.Insert(index: 0, item: interlocutor);
		_offset = _collection.Count;
	}
	#endregion

	#region Instance
	private async Task LoadInterlocutors(
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
		_collection.AddRange(collection: interlocutors.Select(selector: i =>
			Interlocutor.Create(
				client: _client,
				fileService: _fileService,
				id: i.UserId,
				cancellationToken: cancellationToken
			).GetAwaiter().GetResult()
		));
		_offset = _collection.Count;
	}

	internal void OnAppearedOnline(InterlocutorAppearedOnlineEventArgs e)
	{
		InterlocutorAppearedOnline?.Invoke(e: e);
		this[id: e.InterlocutorId].OnAppearedOnline(e: new Interlocutor.AppearedOnlineEventArgs(
			onlineAt: e.OnlineAt
		));
	}

	internal void OnAppearedOffline(InterlocutorAppearedOfflineEventArgs e)
	{
		InterlocutorAppearedOffline?.Invoke(e: e);
		this[id: e.InterlocutorId].OnAppearedOffline(e: new Interlocutor.AppearedOfflineEventArgs(
			onlineAt: e.OnlineAt
		));
	}

	internal void OnUpdatedPhoto(InterlocutorUpdatedPhotoEventArgs e)
	{
		InterlocutorUpdatedPhoto?.Invoke(e: e);
		this[id: e.InterlocutorId].OnUpdatedPhoto(e: new Interlocutor.UpdatedPhotoEventArgs());
	}

	internal void OnDeletedPhoto(InterlocutorDeletedPhotoEventArgs e)
	{
		InterlocutorDeletedPhoto?.Invoke(e: e);
		this[id: e.InterlocutorId].OnDeletedPhoto(e: new Interlocutor.DeletedPhotoEventArgs());
	}
	#endregion
	#endregion
}