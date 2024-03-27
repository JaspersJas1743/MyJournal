namespace MyJournal.API.Assets.Hubs;

public interface IAdministratorHub
{
	Task StudentCompletedTask(int taskId);
	Task StudentUncompletedTask(int taskId);
	Task CreatedTaskToStudents(int taskId);
}