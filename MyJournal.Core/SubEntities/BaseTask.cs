namespace MyJournal.Core.SubEntities;

public sealed record TaskAttachment(string LinkToFile, Attachment.AttachmentType AttachmentType);
public sealed record TaskContent(string? Text, IEnumerable<Attachment>? Attachments);

public abstract class BaseTask : ISubEntity
{
	public int Id { get; init; }
	public DateTime ReleasedAt { get; init; }
	public TaskContent Content { get; init; }
}