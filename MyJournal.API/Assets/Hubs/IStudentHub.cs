namespace MyJournal.API.Assets.Hubs;

public interface IStudentHub
{
	Task CompletedTask(int taskId);
	Task UncompletedTask(int taskId);
	Task TeacherCreatedTask(int taskId, int subjectId);
	Task TeacherCreatedAssessment(int assessmentId, int studentId, int subjectId);
	Task TeacherChangedAssessment(int assessmentId, int studentId, int subjectId);
	Task TeacherDeletedAssessment(int assessmentId, int studentId, int subjectId);
}