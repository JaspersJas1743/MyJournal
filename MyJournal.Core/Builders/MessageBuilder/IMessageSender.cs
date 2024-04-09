namespace MyJournal.Core.Builders.MessageBuilder;

public interface IMessageSender
{
	Task Send(CancellationToken cancellationToken = default(CancellationToken));
}