namespace MyJournal.Core.Utilities.EventArgs;

public class InterlocutorEventArgs(int interlocutorId) : System.EventArgs
{
	public int InterlocutorId { get; } = interlocutorId;
}