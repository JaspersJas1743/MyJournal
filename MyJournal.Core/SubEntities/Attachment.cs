namespace MyJournal.Core.SubEntities;

public sealed class Attachment
{
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
		Photo,
		Document
	}

	internal static async Task<Attachment> Create(
		string? linkToFile,
		AttachmentType type
	) => new Attachment(linkToFile: linkToFile, type: type);
}
