namespace MyJournal.Core.Utilities.EventArgs;

public class ChangeOnlineStatusEventArgs(int interlocutorId, DateTime? onlineAt) : InterlocutorEventArgs(interlocutorId)
{
	public DateTime? OnlineAt { get; } = onlineAt;
}