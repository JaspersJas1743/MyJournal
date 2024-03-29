namespace MyJournal.API.Assets.Hubs;

public interface ITeacherHub
{
	Task StudentCompletedTask(int taskId);
	Task StudentUncompletedTask(int taskId);
	Task CreatedTask(int taskId, int subjectId);
	Task CreatedAssessment(int assessmentId);
	Task ChangedAssessment(int assessmentId);
	Task DeletedAssessment(int assessmentId);
}