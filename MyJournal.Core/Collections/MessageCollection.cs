using MyJournal.Core.Builders.MessageBuilder;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.EventArgs;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.Collections;

public sealed class MessageCollection : LazyCollection<Message>
{
	#region Fields
	private readonly int _chatId;
	private readonly IFileService _fileService;
	private readonly ApiClient _client;
	#endregion

	#region Constructor
	private MessageCollection(
		ApiClient client,
		IFileService fileService,
		int chatId,
		AsyncLazy<List<Message>> messages,
		int count,
		int offset
	) : base(client: client, collection: messages, count: count, offset: offset)
	{
		_client = client;
		_chatId = chatId;
		_fileService = fileService;
	}
	#endregion

	#region Records
	private sealed record GetMessagesRequest(int ChatId, int Offset, int Count);
	#endregion

	#region Events

	public event ReadMessagesHandler ReadMessages;
	#endregion

	#region Methods
	#region Static
	internal static async Task<MessageCollection> Create(
		ApiClient client,
		IFileService fileService,
		int chatId,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		const int basedOffset = 0;
		const int basedCount = 20;
		IEnumerable<Message.GetMessageResponse> messages = await client.GetAsync<IEnumerable<Message.GetMessageResponse>, GetMessagesRequest>(
			apiMethod: MessageControllerMethods.GetMessages,
			argQuery: new GetMessagesRequest(ChatId: chatId, Offset: basedOffset, Count: basedCount),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return new MessageCollection(
			client: client,
			chatId: chatId,
			fileService: fileService,
			messages: new AsyncLazy<List<Message>>(valueFactory: async () => new List<Message>(
				collection: messages.Select(selector: m => Message.Create(
					response: m,
					fileService: fileService
				)).Reverse()
			)),
			count: basedCount,
			offset: messages.Count()
		);
	}
	#endregion

	#region LazyCollection<Message>
	protected override async Task Load(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<Message.GetMessageResponse> loadedMessages = await Client.GetAsync<IEnumerable<Message.GetMessageResponse>, GetMessagesRequest>(
			apiMethod: MessageControllerMethods.GetMessages,
			argQuery: new GetMessagesRequest(ChatId: _chatId, Offset: Offset, Count: Count),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		List<Message> collection = await Collection;
		collection.InsertRange(index: 0, collection: loadedMessages.Select(
			selector: m => Message.Create(
				response: m, fileService: _fileService
			)
		).Reverse());
		Offset = collection.Count;
	}

	internal override async Task Append(
		int id,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await base.Append(
			instance: await Message.Create(
				client: Client,
				messageId: id,
				fileService: _fileService,
				cancellationToken: cancellationToken
			), cancellationToken: cancellationToken
		);
	}

	internal override async Task Insert(
		int index,
		int id,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await base.Insert(
			index: Length - index,
			instance: await Message.Create(
				client: Client,
				messageId: id,
				fileService: _fileService,
				cancellationToken: cancellationToken
			), cancellationToken: cancellationToken
		);
	}
	#endregion

	#region Instance
	public IMessageBuilder CreateMessage()
		=> MessageBuilder.Create(client: _client, fileService: _fileService, chatId: _chatId);

	public async Task OnReadMessages()
	{
		await foreach(Message message in this.Where(predicate: m => m is { IsRead: false, FromMe: true }))
			message.OnReadMessage(e: new ReadMessageEventArgs());

		ReadMessages?.Invoke();
	}
	#endregion
	#endregion
}