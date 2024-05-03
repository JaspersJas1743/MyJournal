using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.Builders.MessageBuilder;

internal sealed class MessageBuilder : IMessageBuilder
{
	private readonly Dictionary<string, Attachment> _attachments = new Dictionary<string, Attachment>();
	private readonly IFileService _fileService;
	private readonly int _chatId;
	private string _text = String.Empty;

	private MessageBuilder(
		IFileService fileService,
		int chatId
	)
	{
		_fileService = fileService;
		_chatId = chatId;
	}

	internal sealed record SendMessageRequest(int ChatId, Message.MessageContent Content);

	internal static MessageBuilder Create(IFileService fileService, int chatId)
		=> new MessageBuilder(fileService: fileService, chatId: chatId);

	public IMessageBuilder SetText(string text)
	{
		_text = text;
		return this;
	}

	public async Task<IMessageBuilder> AddAttachment(
		string pathToFile,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		_attachments.Add(key: pathToFile, value: await Attachment.Create(
			fileService: _fileService,
			pathToFile: pathToFile,
			cancellationToken: cancellationToken
		));
		return this;
	}

	public async Task<IMessageBuilder> RemoveAttachment(
		string pathToFile,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		Attachment attachment = _attachments[key: pathToFile];
		await _fileService.Delete(link: attachment.LinkToFile, cancellationToken: cancellationToken);
		_attachments.Remove(key: pathToFile);
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
						LinkToFile: a.Value.LinkToFile,
						AttachmentType: a.Value.Type
					)
				))
			),
			cancellationToken: cancellationToken
		);
	}
}