using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.SubEntities;

public sealed class Attachment
{
	#region Fields
	private const string DefaultBucket = "message_attachments";
	private static readonly string[] PhotoExtension = new string[] { ".png", ".jpg", ".jpeg" };
	private readonly IFileService _fileService;
	#endregion

	#region Constructor
	private Attachment(
		string? linkToFile,
		AttachmentType type,
		IFileService fileService
	)
	{
		_fileService = fileService;

		LinkToFile = linkToFile;
		Type = type;
	}
	#endregion

	#region Properties
	public string? LinkToFile { get; set; }
	public AttachmentType Type { get; set; }
	#endregion

	#region Enum
	public enum AttachmentType
	{
		Document,
		Photo
	}
	#endregion

	#region Methods
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

		return new Attachment(linkToFile: link, type: type, fileService: fileService);
	}

	internal static Attachment Create(
		string? linkToFile,
		AttachmentType type,
		IFileService fileService
	) => new Attachment(linkToFile: linkToFile, type: type, fileService: fileService);

	public async Task Download(
		string pathToSave,
		CancellationToken cancellationToken = default(CancellationToken)
	) => await _fileService.Download(link: LinkToFile, pathToSave: pathToSave, cancellationToken: cancellationToken);
	#endregion
}
