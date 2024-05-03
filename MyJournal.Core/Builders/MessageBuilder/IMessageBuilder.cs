namespace MyJournal.Core.Builders.MessageBuilder;

public interface IMessageBuilder
{
	IMessageBuilder SetText(string text);
	Task<IMessageBuilder> AddAttachment(string pathToFile, CancellationToken cancellationToken = default(CancellationToken));
	Task<IMessageBuilder> RemoveAttachment(string pathToFile, CancellationToken cancellationToken = default(CancellationToken));
	Task Send(CancellationToken cancellationToken = default(CancellationToken));
}