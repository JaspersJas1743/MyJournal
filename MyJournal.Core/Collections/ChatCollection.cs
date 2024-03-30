using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.Collections;

public sealed class ChatCollection : LazyCollection<Chat>
{
	private readonly IFileService _fileService;

	#region Constructors
	private ChatCollection(
		ApiClient client,
		IFileService fileService,
		AsyncLazy<List<Chat>> chats,
		int count,
		int offset
	) : base(client: client, collection: chats, count: count, offset: offset)
	{
		_fileService = fileService;
	}
	#endregion

	#region Properties
	public string? Filter { get; private set; } = String.Empty;
	#endregion

	#region Records
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
		IFileService fileService,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		const int basedOffset = 0;
		const int basedCount = 20;
		IEnumerable<Chat.ChatResponse> chats = await client.GetAsync<IEnumerable<Chat.ChatResponse>, GetChatsRequest>(
			apiMethod: ChatControllerMethods.GetChats,
			argQuery: new GetChatsRequest(
				IsFiltered: false,
				Filter: String.Empty,
				Offset: basedOffset,
				Count: basedCount
			),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return new ChatCollection(
			client: client,
			fileService: fileService,
			chats: new AsyncLazy<List<Chat>>(valueFactory: async () => new List<Chat>(collection: await Task.WhenAll(
				tasks: chats.Select(selector: async c => await Chat.Create(
					client: client,
					fileService: fileService,
					id: c.Id,
					cancellationToken: cancellationToken
				))
			))),
			count: basedCount,
			offset: chats.Count()
		);
	}
	#endregion

	#region LazyCollection<Chat>
	protected override async Task Load(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<Chat.ChatResponse> loadedChats = await _client.GetAsync<IEnumerable<Chat.ChatResponse>, GetChatsRequest>(
			apiMethod: ChatControllerMethods.GetChats,
			argQuery: new GetChatsRequest(
				IsFiltered: !String.IsNullOrWhiteSpace(value: Filter),
				Filter: Filter,
				Offset: _offset,
				Count: _count
			), cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		List<Chat> collection = await _collection;
		collection.AddRange(collection: await Task.WhenAll(tasks: loadedChats.Select(selector: async c => await Chat.Create(
			client: _client,
			fileService: _fileService,
			id: c.Id,
			cancellationToken: cancellationToken
		))));
		_offset = collection.Count;
	}

	public override async Task Clear(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		Filter = String.Empty;
		await base.Clear(cancellationToken: cancellationToken);
	}

	internal override async Task Append(
		int chatId,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await base.Append(instance: await Chat.Create(
			client: _client,
			fileService: _fileService,
			id: chatId,
			cancellationToken: cancellationToken
		), cancellationToken: cancellationToken);
	}

	internal override async Task Insert(
		int index,
		int chatId,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await base.Insert(index: index, instance: await Chat.Create(
			client: _client,
			fileService: _fileService,
			id: chatId,
			cancellationToken: cancellationToken
		), cancellationToken: cancellationToken);
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
		List<Chat> collection = await _collection;
		collection.AddRange(collection: chats);
		_offset = collection.Count;
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
		List<Chat> collection = await _collection;
		collection.AddRange(collection: chats);
		_offset = collection.Count;
	}

	internal async Task OnReceivedMessage(
		ReceivedMessageInChatEventArgs e,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		Chat chat = await GetById(id: e.ChatId);
		if (chat.MessagesAreCreated)
		{
			MessageCollection messageFromChat = await chat.GetMessages();
			await messageFromChat.Append(id: e.MessageId, cancellationToken: cancellationToken);
		}
		chat.OnReceivedMessage(e: new Chat.ReceivedMessageEventArgs(messageId: e.MessageId));
		ReceivedMessageInChat?.Invoke(e: e);
	}
	#endregion
	#endregion
}