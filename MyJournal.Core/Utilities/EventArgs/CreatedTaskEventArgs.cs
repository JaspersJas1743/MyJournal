namespace MyJournal.Core.Utilities.EventArgs;

public sealed class CreatedTaskEventArgs(int taskId, int subjectId, int classId) : System.EventArgs
{
	public int TaskId { get; } = taskId;
	public int SubjectId { get; } = subjectId;
	public int ClassId { get; } = classId;
}