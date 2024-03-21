using MyJournal.Core.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.Constants.Controllers;

namespace MyJournal.Core.MessageBuilder;

public sealed class MessageSender : IMessageSender
{
	private readonly ApiClient _client;
	private readonly Message.MessageContent _content;
	private readonly int _chatId;

	private MessageSender(
		ApiClient client,
		Message.MessageContent content,
		int chatId
	)
	{
		_client = client;
		_content = content;
		_chatId = chatId;
	}

	public async Task Send(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await _client.PostAsync<MessageCollection.SendMessageRequest>(
			apiMethod: MessageControllerMethods.SendMessage,
			arg: new MessageCollection.SendMessageRequest(
				ChatId: _chatId,
				Content: _content
			),
			cancellationToken: cancellationToken
		);
	}

	internal static MessageSender Create(
		ApiClient client,
		Message.MessageContent content,
		int chatId,
		CancellationToken cancellationToken = default(CancellationToken)
	) => new MessageSender(client: client, content: content, chatId: chatId);
}
