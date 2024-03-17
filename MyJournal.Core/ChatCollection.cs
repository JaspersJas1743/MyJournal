using System.Collections;
using MyJournal.Core.Utilities;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core;

public sealed class ChatCollection : IEnumerable<Chat>
{
	#region Fields
	private readonly ApiClient _client;
	private readonly List<Chat> _chats = new List<Chat>();
	private readonly int _count;

	private int _offset;
	private string _filter = String.Empty;
	#endregion

	#region Constructors
	private ChatCollection(ApiClient client, IEnumerable<Chat> chats, int count)
	{
		_client = client;
		_chats.AddRange(collection: chats);
		_offset = _chats.Count;
		_count = count;
	}
	#endregion

	#region Properties
	public int Length => _chats.Count;

	public string Filter => _filter;

	public Chat this[int index]
		=> _chats.ElementAtOrDefault(index: index)
		?? throw new ArgumentOutOfRangeException(message: $"{index} элемент отсутствует или не загружен.", paramName: nameof(index));
	#endregion

	#region Records
	private sealed record GetChatsRequest(bool IsFiltered, string? Filter, int Offset, int Count);
	private sealed record UploadChatPhotoResponse(string Link);
	private sealed record DeleteChatPhotoRequest(string Link);
	private sealed record CreateSingleChatRequest(int InterlocutorId);
	private sealed record CreateMultiChatRequest(IEnumerable<int> InterlocutorIds, string? ChatName, string? LinkToPhoto);
	#endregion

	#region Methods
	#region Static
	public static async Task<ChatCollection> Create(
		ApiClient client,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		const int basedOffset = 0;
		const int basedCount = 20;
		IEnumerable<Chat> chats = await client.GetAsync<IEnumerable<Chat>, GetChatsRequest>(
			apiMethod: ChatsControllerMethods.GetChats,
			argQuery: new GetChatsRequest(IsFiltered: false, Filter: String.Empty, Offset: basedOffset, Count: basedCount),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return new ChatCollection(client: client, chats: chats.ToList(), count: basedCount);
	}
	#endregion

	#region Instance
	private async Task LoadChats(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<Chat> loadedChats = await _client.GetAsync<IEnumerable<Chat>, GetChatsRequest>(
			apiMethod: ChatsControllerMethods.GetChats,
			argQuery: new GetChatsRequest(
				IsFiltered: !String.IsNullOrWhiteSpace(value: _filter),
				Filter: _filter,
				Offset: _offset,
				Count: _count
			), cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		_chats.AddRange(collection: loadedChats);
		_offset = _chats.Count;
	}

	public async Task LoadNext(
		CancellationToken cancellationToken = default(CancellationToken)
	) => await LoadChats(cancellationToken: cancellationToken);

	public async Task Clear(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		_chats.Clear();
		_offset = _chats.Count;
		_filter = String.Empty;
	}

	public async Task SetFilter(
		string filter,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await Clear(cancellationToken: cancellationToken);
		_filter = filter;
		await LoadChats(cancellationToken: cancellationToken);
	}

	public async Task Append(
		int chatId,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		Chat chat = await _client.GetAsync<Chat>(
			apiMethod: ChatsControllerMethods.GetChat(chatId: chatId),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		_chats.Insert(index: 0, item: chat);
		_offset = _chats.Count;
	}

	public async Task AddSingleChat(
		int interlocutorId,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await Clear(cancellationToken: cancellationToken);
		IEnumerable<Chat> chats = await _client.PostAsync<IEnumerable<Chat>, CreateSingleChatRequest>(
			apiMethod: ChatsControllerMethods.CreateSingleChat,
			arg: new CreateSingleChatRequest(InterlocutorId: interlocutorId),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		_chats.AddRange(collection: chats);
		_offset = _chats.Count;
	}

	public async Task AddMultiChat(
		IEnumerable<int> interlocutorIds,
		string? chatName,
		string linkToPhoto,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await Clear(cancellationToken: cancellationToken);
		IEnumerable<Chat> chats = await _client.PostAsync<IEnumerable<Chat>, CreateMultiChatRequest>(
			apiMethod: ChatsControllerMethods.CreateMultiChat,
			arg: new CreateMultiChatRequest(InterlocutorIds: interlocutorIds, ChatName: chatName, LinkToPhoto: linkToPhoto),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		_chats.AddRange(collection: chats);
		_offset = _chats.Count;
	}

	public async Task<string> UploadMultiChatPhoto(
		string pathToPhoto,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		UploadChatPhotoResponse? response = await _client.PutFileAsync<UploadChatPhotoResponse>(
			apiMethod: ChatsControllerMethods.UploadChatPhoto,
			path: pathToPhoto,
			cancellationToken: cancellationToken
		);
		return response!.Link;
	}

	public async Task DeleteMultiChatPhoto(
		string link,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await _client.DeleteAsync<DeleteChatPhotoRequest>(
			apiMethod: ChatsControllerMethods.DeleteChatPhoto,
			arg: new DeleteChatPhotoRequest(Link: link),
			cancellationToken: cancellationToken
		);
	}
	#endregion

	#region IEnumerable<Chat>
	public IEnumerator<Chat> GetEnumerator()
		=> _chats.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> GetEnumerator();
	#endregion

	#region Overriden
	public override bool Equals(object? obj)
	{
		if (obj is ChatCollection collection)
			return this.SequenceEqual(second: collection);
		return false;
	}
	#endregion
	#endregion
}