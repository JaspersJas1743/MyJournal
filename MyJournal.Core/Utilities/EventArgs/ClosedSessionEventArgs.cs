namespace MyJournal.Core.Utilities.EventArgs;

public sealed class ClosedSessionEventArgs(IEnumerable<int> sessionIds, bool currentSessionAreClosed) : System.EventArgs
{
	public IEnumerable<int> SessionIds { get; } = sessionIds;
	public bool CurrentSessionAreClosed { get; } = currentSessionAreClosed;
}
