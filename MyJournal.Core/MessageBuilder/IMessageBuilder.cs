namespace MyJournal.Core.MessageBuilder;

public interface IMessageBuilder
{
	IMessageBuilder AddText(string text);
	Task<IMessageBuilder> AddAttachment(string pathToFile, CancellationToken cancellationToken = default(CancellationToken));
	Task<IMessageBuilder> RemoveAttachment(int index, CancellationToken cancellationToken = default(CancellationToken));
	IMessageSender Build();
}