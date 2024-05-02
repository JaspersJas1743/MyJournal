using System.Text;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.Builders.MessageBuilder;

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
			text: text,
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
			text: String.Empty,
			attachments: new Attachment[] { attachment },
			chatId: _chatId
		);
	}
}