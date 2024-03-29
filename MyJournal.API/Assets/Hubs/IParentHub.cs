namespace MyJournal.API.Assets.Hubs;

public interface IParentHub
{
	Task WardCompletedTask(int taskId);
	Task WardUncompletedTask(int taskId);
	Task CreatedTaskToWard(int taskId, int subjectId);
	Task CreatedAssessmentToWard(int assessmentId);
	Task ChangedAssessmentToWard(int assessmentId);
	Task DeletedAssessmentToWard(int assessmentId);
}