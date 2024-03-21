using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.Collections;

public sealed class MessageCollection : LazyCollection<Message>
{
	#region Fields

	private readonly int _chatId;
	#endregion

	#region Constructor
	private MessageCollection(
		ApiClient client,
		int chatId,
		IEnumerable<Message> messages,
		int count
	) : base(client: client, collection: messages, count: count)
	{
		_chatId = chatId;
	}
	#endregion

	#region Records
	private sealed record GetMessagesRequest(int ChatId, int Offset, int Count);
	internal sealed record SendMessageRequest(int ChatId, Message.MessageContent Content);
	#endregion

	#region Methods
	#region Static
	internal static async Task<MessageCollection> Create(
		ApiClient client,
		int chatId,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		const int basedOffset = 0;
		const int basedCount = 20;
		IEnumerable<Message.GetMessageResponse> messages = await client.GetAsync<IEnumerable<Message.GetMessageResponse>, GetMessagesRequest>(
			apiMethod: MessageControllerMethods.GetMessages,
			argQuery: new GetMessagesRequest(
				ChatId: chatId,
				Offset: basedOffset,
				Count: basedCount
			),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return new MessageCollection(
			client: client,
			chatId: chatId,
			messages: messages.Select(m =>
				Message.Create(
					client: client,
					messageId: m.MessageId,
					cancellationToken: cancellationToken
				).GetAwaiter().GetResult()
			).Reverse(),
			count: basedCount
		);
	}
	#endregion

	#region LazyCollection<Message>
	protected override async Task Load(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		IEnumerable<Message.GetMessageResponse> loadedMessages = await _client.GetAsync<IEnumerable<Message.GetMessageResponse>, GetMessagesRequest>(
			apiMethod: ChatControllerMethods.GetChats,
			argQuery: new GetMessagesRequest(
				ChatId: _chatId,
				Offset: _offset,
				Count: _count
			), cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		_collection.Value.InsertRange(index: 0, collection: loadedMessages.Select(selector: m =>
			Message.Create(
				client: _client,
				messageId: m.MessageId,
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
		await base.Insert(index: 0, instance: await Message.Create(
			client: _client,
			messageId: id,
			cancellationToken: cancellationToken
		), cancellationToken: cancellationToken);
	}

	internal override async Task Insert(
		int index,
		int id,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await base.Insert(index: Length - index, instance: await Message.Create(
			client: _client,
			messageId: id,
			cancellationToken: cancellationToken
		), cancellationToken: cancellationToken);
	}
	#endregion

	#region Instance
	public async Task Send(
		string? text = null,
		IEnumerable<Attachment>? attachments = null,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await _client.PostAsync<SendMessageRequest>(
			apiMethod: MessageControllerMethods.SendMessage,
			arg: new SendMessageRequest(
				ChatId: _chatId,
				Content: new Message.MessageContent(
					Text: text,
					Attachments: attachments?.Select(
						selector: a => new Message.MessageAttachment(LinkToFile: a.LinkToFile, Type: a.Type)
				))
			),
			cancellationToken: cancellationToken
		);
	}
	#endregion
	#endregion
}