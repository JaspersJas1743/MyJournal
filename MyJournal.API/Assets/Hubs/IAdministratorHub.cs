namespace MyJournal.API.Assets.Hubs;

public interface IAdministratorHub
{
	Task StudentCompletedTask(int taskId);
	Task StudentUncompletedTask(int taskId);
	Task CreatedTaskToStudents(int taskId, int subjectId, int classId);
	Task CreatedAssessmentToStudent(int assessmentId, int studentId, int subjectId);
	Task ChangedAssessmentToStudent(int assessmentId, int studentId, int subjectId);
	Task DeletedAssessmentToStudent(int assessmentId, int studentId, int subjectId);
	Task ChangedTimetable(int classId);
}