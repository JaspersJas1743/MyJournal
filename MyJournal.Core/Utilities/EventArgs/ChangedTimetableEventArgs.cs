namespace MyJournal.Core.Utilities.EventArgs;

public sealed class ChangedTimetableEventArgs(int classId, IEnumerable<int> subjectIds) : System.EventArgs
{
	public int ClassId { get; } = classId;
	public IEnumerable<int> SubjectIds { get; } = subjectIds;
}