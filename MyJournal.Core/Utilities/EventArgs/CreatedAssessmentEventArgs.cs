namespace MyJournal.Core.Utilities.EventArgs;

public sealed class CreatedAssessmentEventArgs(int assessmentId, int studentId, int subjectId)
	: AssessmentEventArgs(assessmentId, studentId, subjectId);