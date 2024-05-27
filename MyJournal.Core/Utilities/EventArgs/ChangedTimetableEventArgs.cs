namespace MyJournal.Core.Utilities.EventArgs;

public sealed class ChangedTimetableEventArgs(int classId) : System.EventArgs
{
	public int ClassId { get; } = classId;
}