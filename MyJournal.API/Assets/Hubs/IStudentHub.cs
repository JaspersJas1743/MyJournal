namespace MyJournal.API.Assets.Hubs;

public interface IStudentHub
{
	Task CompletedTask(int taskId);
	Task UncompletedTask(int taskId);
	Task TeacherCreatedTask(int taskId, int subjectId);
	Task TeacherCreatedAssessment(int assessmentId);
	Task TeacherChangedAssessment(int assessmentId);
	Task TeacherDeletedAssessment(int assessmentId);
}