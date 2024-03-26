using System.Text;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.MessageBuilder;

internal sealed class MessageBuilder : IMessageBuilder
{
	private readonly StringBuilder _text = new StringBuilder();
	private readonly List<Attachment> _attachments = new List<Attachment>();
	private readonly IFileService _fileService;
	private readonly int _chatId;

	private MessageBuilder(
		IFileService fileService,
		StringBuilder builder,
		IEnumerable<Attachment> attachments,
		int chatId
	)
	{
		_text.Append(value: builder);
		_attachments.AddRange(collection: attachments);
		_fileService = fileService;
		_chatId = chatId;
	}

	internal static MessageBuilder Create(
		IFileService fileService,
		StringBuilder builder,
		IEnumerable<Attachment> attachments,
		int chatId
	) => new MessageBuilder(fileService: fileService, builder: builder, attachments: attachments, chatId: chatId);

	public IMessageBuilder AddText(
		string text
	)
	{
		_text.Append(value: text);
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

	public IMessageSender Build()
	{
		return MessageSender.Create(
			client: _fileService.ApiClient,
			content: new Message.MessageContent(
				Text: _text.ToString(),
				Attachments: _attachments.Select(selector: a =>
					new Message.MessageAttachment(
						LinkToFile: a.LinkToFile,
						AttachmentType: a.Type
					)
				)
			),
			chatId: _chatId
		);
	}
}