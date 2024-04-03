namespace MyJournal.Core.Utilities.EventArgs;

public sealed class ReceivedMessageEventArgs(int chatId, int messageId) : System.EventArgs
{
	public int ChatId { get; } = chatId;
	public int MessageId { get; } = messageId;
}