namespace MyJournal.Core.SubEntities;

public interface ITextContent
{
	string? Text { get; }
}

public interface IFileContent
{
	IEnumerable<Attachment>? Attachments { get; }
}

public class TextContent(string? text) : ITextContent
{
	public string? Text { get; } = text;
}

public class FileContent(IEnumerable<Attachment>? files) : IFileContent
{
	public IEnumerable<Attachment>? Attachments { get; } = files;
}

public class TextContentWithAttachments(string? text, IEnumerable<Attachment>? files) : ITextContent, IFileContent
{
	public string? Text { get; } = text;
	public IEnumerable<Attachment>? Attachments { get; } = files;
}