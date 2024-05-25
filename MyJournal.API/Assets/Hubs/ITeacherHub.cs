namespace MyJournal.API.Assets.Hubs;

public interface ITeacherHub
{
	Task StudentCompletedTask(int taskId);
	Task StudentUncompletedTask(int taskId);
	Task CreatedTask(int taskId, int subjectId);
	Task CreatedFinalAssessment(int assessmentId, int studentId, int subjectId, int periodId);
	Task CreatedAssessment(int assessmentId, int studentId, int subjectId);
	Task ChangedAssessment(int assessmentId, int studentId, int subjectId);
	Task DeletedAssessment(int assessmentId, int studentId, int subjectId);
	Task ChangedTimetable(int classId);
}