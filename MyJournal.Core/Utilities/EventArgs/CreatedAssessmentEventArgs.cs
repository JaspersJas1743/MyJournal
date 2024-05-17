namespace MyJournal.Core.Utilities.EventArgs;

public class CreatedAssessmentEventArgs(int assessmentId, int studentId, int subjectId)
	: AssessmentEventArgs(assessmentId: assessmentId, studentId: studentId, subjectId: subjectId);