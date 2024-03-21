using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.Collections;

public sealed class ChatCollection : LazyCollection<Chat>
{
	#region Constructors
	private ChatCollection(
		ApiClient client,
		IEnumerable<Chat> chats,
		int count
	) : base(client: client, collection: chats, count: count)
	{ }
	#endregion

	#region Properties
	public string? Filter { get; private set; } = String.Empty;
	#endregion

	#region Records
	private sealed record ChatResponse(int Id, string? ChatName, string? ChatPhoto, LastMessage? LastMessage, int CountOfUnreadMessages);
	private sealed record GetChatsRequest(bool IsFiltered, string? Filter, int Offset, int Count);
	private sealed record CreateSingleChatRequest(int InterlocutorId);
	private sealed record CreateMultiChatRequest(IEnumerable<int> InterlocutorIds, string? ChatName, string? LinkToPhoto);
	#endregion

	#region Classes
	public sealed class ReceivedMessageInChatEventArgs(int chatId, int messageId) : EventArgs
	{
		public int ChatId { get; } = chatId;
		public int MessageId { get; } = messageId;
	}
	#endregion

	#region Delegates
	public delegate void ReceivedMessageInChatHandler(ReceivedMessageInChatEventArgs e);
	#endregion

	#region Events
	public event ReceivedMessageInChatHandler? ReceivedMessageInChat;
	#endregion

	#region Methods
	#region Static
	internal static async Task<ChatCollection> Create(
		ApiClient client,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		const int basedOffset = 0;
		const int basedCount = 20;
		IEnumerable<ChatResponse> chats = await client.GetAsync<IEnumerable<ChatResponse>, GetChatsRequest>(
			apiMethod: ChatControllerMethods.GetChats,
			argQuery: new GetChatsRequest(IsFiltered: false, Filter: String.Empty, Offset: basedOffset, Count: basedCount),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return new ChatCollection(
			client: client,
			chats: chats.Select(selector: c =>
				Chat.Create(
					client: client,
					id: c.Id,
					cancellationToken: cancellationToken
				).GetAwaiter().GetResult()
			),
			count: basedCount
		);
	}
	#endregion

	#region LazyCollection<Chat>
	public override async Task LoadNext(
		CancellationToken cancellationToken = default(CancellationToken)
	) => await LoadChats(cancellationToken: cancellationToken);

	public override async Task Clear(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		_collection.Value.Clear();
		_offset = _collection.Value.Count;
		Filter = String.Empty;
	}

	public override async Task Append(
		int chatId,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		Chat chat = await _client.GetAsync<Chat>(
			apiMethod: ChatControllerMethods.GetChat(chatId: chatId),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		_collection.Value.Insert(index: 0, item: chat);
		_offset = _collection.Value.Count;
	}
	#endregion

	#region Instance
	private async Task LoadChats(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<ChatResponse> loadedChats = await _client.GetAsync<IEnumerable<ChatResponse>, GetChatsRequest>(
			apiMethod: ChatControllerMethods.GetChats,
			argQuery: new GetChatsRequest(
				IsFiltered: !String.IsNullOrWhiteSpace(value: Filter),
				Filter: Filter,
				Offset: _offset,
				Count: _count
			), cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		_collection.Value.AddRange(collection: loadedChats.Select(selector: c =>
			Chat.Create(
				client: _client,
				id: c.Id,
				cancellationToken: cancellationToken
			).GetAwaiter().GetResult()
		));
		_offset = _collection.Value.Count;
	}

	public async Task SetFilter(
		string? filter,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await Clear(cancellationToken: cancellationToken);
		Filter = filter;
		await LoadChats(cancellationToken: cancellationToken);
	}

	public async Task AddSingleChat(
		int interlocutorId,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await Clear(cancellationToken: cancellationToken);
		IEnumerable<Chat> chats = await _client.PostAsync<IEnumerable<Chat>, CreateSingleChatRequest>(
			apiMethod: ChatControllerMethods.CreateSingleChat,
			arg: new CreateSingleChatRequest(InterlocutorId: interlocutorId),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		_collection.Value.AddRange(collection: chats);
		_offset = _collection.Value.Count;
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
			apiMethod: ChatControllerMethods.CreateMultiChat,
			arg: new CreateMultiChatRequest(InterlocutorIds: interlocutorIds, ChatName: chatName, LinkToPhoto: linkToPhoto),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		_collection.Value.AddRange(collection: chats);
		_offset = _collection.Value.Count;
	}

	internal async Task OnReceivedMessage(
		ReceivedMessageInChatEventArgs e,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await this[id: e.ChatId].Messages.Append(id: e.MessageId, cancellationToken: cancellationToken);
		this[id: e.ChatId].OnReceivedMessage(e: new Chat.ReceivedMessageEventArgs(messageId: e.MessageId));
		ReceivedMessageInChat?.Invoke(e: e);
	}
	#endregion
	#endregion
}