using System.Collections;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.Interlocutors;

public class InterlocutorCollection : IEnumerable<Interlocutor>
{
	#region Fields
	private readonly ApiClient _client;
	private readonly IFileService _fileService;
	private readonly List<Interlocutor> _interlocutors = new List<Interlocutor>();
	private readonly int _count;

	private int _offset;
	private string? _filter = String.Empty;
	private bool _includeExistedInterlocutors = false;
	#endregion

	#region Constructors
	private InterlocutorCollection(
		ApiClient client,
		IFileService fileService,
		IEnumerable<Interlocutor> interlocutors,
		int count,
		bool includeExistedInterlocutors
	)
	{
		_client = client;
		_fileService = fileService;
		_interlocutors.AddRange(collection: interlocutors);
		_offset = _interlocutors.Count;
		_count = count;

		_includeExistedInterlocutors = includeExistedInterlocutors;
	}
	#endregion

	#region Properties
	public bool IncludeExistedInterlocutors => _includeExistedInterlocutors;

	public string? Filter => _filter;

	public int Length => _interlocutors.Count;

	public Interlocutor this[int id]
		=> _interlocutors.Find(match: i => i.Id.Equals(id))
		?? throw new ArgumentOutOfRangeException(message: $"Собеседник с идентификатором {id} отсутствует или не загружен.", paramName: nameof(id));
	#endregion

	#region Records
	private sealed record GetInterlocutorsRequest(bool IncludeExistedInterlocutors, bool IsFiltered, string? Filter, int Offset, int Count);

	private sealed record GetInterlocutorsResponse(int UserId, string? Photo, string? Name);
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
		bool includeExistedInterlocutors = false,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		const int basedOffset = 0;
		const int basedCount = 20;
		IEnumerable<GetInterlocutorsResponse> interlocutors = await client.GetAsync<IEnumerable<GetInterlocutorsResponse>, GetInterlocutorsRequest>(
			apiMethod: ChatControllerMethods.GetInterlocutors,
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
			fileService: fileService,
			interlocutors: interlocutors.Select(selector: i =>
				Interlocutor.Create(
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
	private async Task LoadInterlocutors(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<GetInterlocutorsResponse> interlocutors = await _client.GetAsync<IEnumerable<GetInterlocutorsResponse>, GetInterlocutorsRequest>(
			apiMethod: ChatControllerMethods.GetInterlocutors,
			argQuery: new GetInterlocutorsRequest(
				IsFiltered: !String.IsNullOrWhiteSpace(value: _filter),
				Filter: _filter,
				Offset: _offset,
				Count: _count,
				IncludeExistedInterlocutors: IncludeExistedInterlocutors
			), cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		_interlocutors.AddRange(collection: interlocutors.Select(selector: i =>
			Interlocutor.Create(
				client: _client,
				fileService: _fileService,
				id: i.UserId,
				cancellationToken: cancellationToken
			).GetAwaiter().GetResult()
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
		string? filter,
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