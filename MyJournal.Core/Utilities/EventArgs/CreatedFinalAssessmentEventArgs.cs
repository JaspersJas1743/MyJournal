namespace MyJournal.Core.Utilities.EventArgs;

public sealed class CreatedFinalAssessmentEventArgs(int assessmentId, int studentId, int subjectId, int periodId)
	: CreatedAssessmentEventArgs(assessmentId, studentId, subjectId)
{
	public int PeriodId { get; } = periodId;
	internal Func<int, string> ApiMethodForFinalAT { get; set; }
	internal string ApiMethodForFinalSP { get; set; }
}