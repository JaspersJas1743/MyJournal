using System.Text;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.MessageBuilder;

public sealed class InitMessageBuilder : IInitMessageBuilder
{
	private readonly IFileService _fileService;

	private InitMessageBuilder(
		IFileService fileService
	) => _fileService = fileService;

	internal static InitMessageBuilder Create(
		IFileService fileService
	) => new InitMessageBuilder(fileService: fileService);

	public IMessageBuilder WithText(
		string text
	)
	{
		return MessageBuilder.Create(
			fileService: _fileService,
			builder: new StringBuilder(value: text),
			attachments: Enumerable.Empty<Attachment>(),
			chatId: -1
		);
	}

	public async Task<IMessageBuilder> WithAttachment(
		string pathToFile,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		Attachment attachment = await Attachment.Create(
			fileService: _fileService,
			pathToFile: pathToFile,
			cancellationToken: cancellationToken
		);
		return MessageBuilder.Create(
			fileService: _fileService,
			builder: new StringBuilder(),
			attachments: new Attachment[] { attachment },
			chatId: -1
		);
	}

	public IMessageBuilder ToChat(
		int chatId
	)
	{
		return MessageBuilder.Create(
			fileService: _fileService,
			builder: new StringBuilder(),
			attachments: Enumerable.Empty<Attachment>(),
			chatId: chatId
		);
	}

	public IMessageBuilder ToChat(
		Chat chat
	)
	{
		return MessageBuilder.Create(
			fileService: _fileService,
			builder: new StringBuilder(),
			attachments: Enumerable.Empty<Attachment>(),
			chatId: chat.Id
		);
	}
}