namespace MyJournal.Core.Utilities.EventArgs;

public class AssessmentEventArgs(int assessmentId, int studentId, int subjectId) : System.EventArgs
{
	public int AssessmentId { get; } = assessmentId;
	public int StudentId { get; } = studentId;
	public int SubjectId { get; } = subjectId;
	internal string ApiMethod { get; set; }
}

public sealed class CreatedAssessmentEventArgs(int assessmentId, int studentId, int subjectId)
	: AssessmentEventArgs(assessmentId, studentId, subjectId);

public sealed class ChangedAssessmentEventArgs(int assessmentId, int studentId, int subjectId)
	: AssessmentEventArgs(assessmentId, studentId, subjectId);

public sealed class DeletedAssessmentEventArgs(int assessmentId, int studentId, int subjectId)
	: AssessmentEventArgs(assessmentId, studentId, subjectId);