namespace MyJournal.Core.Builders.MessageBuilder;

public interface IInitMessageBuilder
{
	IMessageBuilder WithText(string text);
	Task<IMessageBuilder> WithAttachment(string pathToFile, CancellationToken cancellationToken = default(CancellationToken));
}