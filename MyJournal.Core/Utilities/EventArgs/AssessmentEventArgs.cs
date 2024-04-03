namespace MyJournal.Core.Utilities.EventArgs;

public class AssessmentEventArgs(int assessmentId, int studentId, int subjectId) : System.EventArgs
{
	public int AssessmentId { get; } = assessmentId;
	public int StudentId { get; } = studentId;
	public int SubjectId { get; } = subjectId;
	internal string ApiMethod { get; set; }
}