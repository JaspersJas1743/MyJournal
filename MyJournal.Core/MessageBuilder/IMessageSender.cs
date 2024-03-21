namespace MyJournal.Core.MessageBuilder;

public interface IMessageSender
{
	Task Send(CancellationToken cancellationToken = default(CancellationToken));
}