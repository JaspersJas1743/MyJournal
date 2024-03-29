namespace MyJournal.API.Assets.Hubs;

public interface IAdministratorHub
{
	Task StudentCompletedTask(int taskId);
	Task StudentUncompletedTask(int taskId);
	Task CreatedTaskToStudents(int taskId, int subjectId, int classId);
	Task CreatedAssessmentToStudent(int assessmentId, int studentId);
	Task ChangedAssessmentToStudent(int assessmentId, int studentId);
	Task DeletedAssessmentToStudent(int assessmentId, int studentId);
}