namespace MyJournal.Core.Utilities.EventArgs;

public sealed class CreatedSessionEventArgs(int sessionId) : System.EventArgs
{
	public int SessionId { get; } = sessionId;
}