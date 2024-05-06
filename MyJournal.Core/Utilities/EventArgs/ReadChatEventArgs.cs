namespace MyJournal.Core.Utilities.EventArgs;

public sealed class ReadChatEventArgs(int chatId) : System.EventArgs
{
	public int ChatId { get; } = chatId;
}