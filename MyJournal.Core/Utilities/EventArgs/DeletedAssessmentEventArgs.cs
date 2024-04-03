namespace MyJournal.Core.Utilities.EventArgs;

public sealed class DeletedAssessmentEventArgs(int assessmentId, int studentId, int subjectId)
	: AssessmentEventArgs(assessmentId, studentId, subjectId);