namespace MyJournal.Core.Utilities.EventArgs;

public sealed class CompletedTaskEventArgs(int taskId) : System.EventArgs
{
	public int TaskId { get; } = taskId;
}