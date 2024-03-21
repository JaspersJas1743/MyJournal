using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.SubEntities;

public sealed class Attachment
{
	private const string DefaultBucket = "message_attachments";

	private static readonly string[] PhotoExtension = new string[] { ".png", ".jpg", ".jpeg" };

	private string _pathToFile { get; }

	private Attachment(
		string? linkToFile,
		AttachmentTypes type
	)
	{
		LinkToFile = linkToFile;
		Type = type;
	}

	private Attachment(
		string pathToFile
	)
	{
		_pathToFile = pathToFile;
	}

	public string? LinkToFile { get; set; }
	public AttachmentTypes Type { get; set; }

	public enum AttachmentTypes
	{
		Document,
		Photo
	}

	internal async Task<Message.MessageAttachment> Load(
		IFileService fileService,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		string link = await fileService.Upload(
			folderToSave: DefaultBucket,
			pathToFile: _pathToFile,
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();

		AttachmentTypes type = AttachmentTypes.Photo;
		if (!PhotoExtension.Contains(Path.GetExtension(path: link)))
			type = AttachmentTypes.Document;

		return new Message.MessageAttachment(LinkToFile: link, Type: type);
	}

	public static async Task<Attachment> Create(
		string pathToFile,
		CancellationToken cancellationToken = default(CancellationToken)
	) => new Attachment(pathToFile);

	internal static Attachment Create(
		string? linkToFile,
		AttachmentTypes type
	) => new Attachment(linkToFile: linkToFile, type: type);
}
