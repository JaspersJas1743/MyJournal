namespace MyJournal.Core.Utilities.EventArgs;

public sealed class JoinedInChatEventArgs(int chatId) : System.EventArgs
{
	public int ChatId { get; } = chatId;
}