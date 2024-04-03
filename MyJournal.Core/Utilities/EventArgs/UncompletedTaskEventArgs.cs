namespace MyJournal.Core.Utilities.EventArgs;

public sealed class UncompletedTaskEventArgs(int taskId) : System.EventArgs
{
	public int TaskId { get; } = taskId;
}
