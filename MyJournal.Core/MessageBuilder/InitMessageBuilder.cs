using System.Text;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.MessageBuilder;

internal sealed class InitMessageBuilder : IInitMessageBuilder
{
	private readonly IFileService _fileService;
	private readonly int _chatId;

	private InitMessageBuilder(
		IFileService fileService,
		int chatId
	)
	{
		_fileService = fileService;
		_chatId = chatId;
	}

	internal static InitMessageBuilder Create(
		IFileService fileService,
		int chatId
	) => new InitMessageBuilder(fileService: fileService, chatId: chatId);

	public IMessageBuilder WithText(
		string text
	)
	{
		return MessageBuilder.Create(
			fileService: _fileService,
			builder: new StringBuilder(value: text),
			attachments: Enumerable.Empty<Attachment>(),
			chatId: _chatId
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
			chatId: _chatId
		);
	}
}