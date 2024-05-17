namespace MyJournal.API.Assets.Hubs;

public interface IParentHub
{
	Task WardCompletedTask(int taskId);
	Task WardUncompletedTask(int taskId);
	Task CreatedTaskToWard(int taskId, int subjectId);
	Task CreatedFinalAssessmentToWard(int assessmentId, int studentId, int subjectId, int periodId);
	Task CreatedAssessmentToWard(int assessmentId, int studentId, int subjectId);
	Task ChangedAssessmentToWard(int assessmentId, int studentId, int subjectId);
	Task DeletedAssessmentToWard(int assessmentId, int studentId, int subjectId);
	Task ChangedTimetable(IEnumerable<int> subjectIds);
}