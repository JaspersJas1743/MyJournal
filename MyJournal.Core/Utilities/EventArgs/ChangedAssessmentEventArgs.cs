namespace MyJournal.Core.Utilities.EventArgs;

public sealed class ChangedAssessmentEventArgs(int assessmentId, int studentId, int subjectId)
	: AssessmentEventArgs(assessmentId, studentId, subjectId);