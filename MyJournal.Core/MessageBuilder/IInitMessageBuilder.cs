using MyJournal.Core.SubEntities;

namespace MyJournal.Core.MessageBuilder;

public interface IInitMessageBuilder
{
	IMessageBuilder WithText(string text);
	Task<IMessageBuilder> WithAttachment(string pathToFile, CancellationToken cancellationToken = default(CancellationToken));
	IMessageBuilder ToChat(int chatId);
	IMessageBuilder ToChat(Chat chat);
}