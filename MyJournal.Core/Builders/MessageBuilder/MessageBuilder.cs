using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.Builders.MessageBuilder;

internal sealed class MessageBuilder : IMessageBuilder
{
	private readonly List<Attachment> _attachments = new List<Attachment>();
	private readonly IFileService _fileService;
	private readonly int _chatId;
	private string _text;

	private MessageBuilder(
		IFileService fileService,
		string text,
		IEnumerable<Attachment> attachments,
		int chatId
	)
	{
		_text = text;
		_attachments.AddRange(collection: attachments);
		_fileService = fileService;
		_chatId = chatId;
	}

	internal sealed record SendMessageRequest(int ChatId, Message.MessageContent Content);

	internal static MessageBuilder Create(
		IFileService fileService,
		string text,
		IEnumerable<Attachment> attachments,
		int chatId
	) => new MessageBuilder(fileService: fileService, text: text, attachments: attachments, chatId: chatId);

	public IMessageBuilder ChangeText(
		string text
	)
	{
		_text = text;
		return this;
	}

	public async Task<IMessageBuilder> AddAttachment(
		string pathToFile,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		_attachments.Add(item: await Attachment.Create(
			fileService: _fileService,
			pathToFile: pathToFile,
			cancellationToken: cancellationToken
		));
		return this;
	}

	public async Task<IMessageBuilder> RemoveAttachment(
		int index,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		Attachment attachment = _attachments[index: index];
		await _fileService.Delete(link: attachment.LinkToFile, cancellationToken: cancellationToken);
		_attachments.Remove(attachment);
		return this;
	}

	public async Task Send(CancellationToken cancellationToken = default(CancellationToken))
	{
		await _fileService.ApiClient.PostAsync<SendMessageRequest>(
			apiMethod: MessageControllerMethods.SendMessage,
			arg: new SendMessageRequest(
				ChatId: _chatId,
				Content: new Message.MessageContent(Text: _text.ToString(), Attachments: _attachments.Select(selector: a =>
					new Message.MessageAttachment(
						LinkToFile: a.LinkToFile,
						AttachmentType: a.Type
					)
				))
			),
			cancellationToken: cancellationToken
		);
	}
}