namespace MyJournal.API.Assets.Hubs;

public interface ITeacherHub
{
	Task StudentCompletedTask(int taskId);
	Task StudentUncompletedTask(int taskId);
	Task CreatedTask(int taskId, int subjectId);
}