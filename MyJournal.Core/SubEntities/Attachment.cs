using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.SubEntities;

public sealed class Attachment
{
	private const string DefaultBucket = "message_attachments";

	private static readonly string[] PhotoExtension = new string[] { ".png", ".jpg", ".jpeg" };

	private Attachment(
		string? linkToFile,
		AttachmentType type
	)
	{
		LinkToFile = linkToFile;
		Type = type;
	}

	public string? LinkToFile { get; set; }
	public AttachmentType Type { get; set; }

	public enum AttachmentType
	{
		Document,
		Photo
	}

	internal static async Task<Attachment> Create(
		IFileService fileService,
		string pathToFile,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		string link = await fileService.Upload(
			folderToSave: DefaultBucket,
			pathToFile: pathToFile,
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();

		AttachmentType type = AttachmentType.Document;
		if (PhotoExtension.Contains(Path.GetExtension(path: pathToFile)))
			type = AttachmentType.Photo;

		return new Attachment(linkToFile: link, type: type);
	}

	internal static Attachment Create(
		string? linkToFile,
		AttachmentType type
	) => new Attachment(linkToFile: linkToFile, type: type);
}
