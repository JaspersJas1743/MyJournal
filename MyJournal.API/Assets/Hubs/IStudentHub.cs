namespace MyJournal.API.Assets.Hubs;

public interface IStudentHub
{
	Task CompletedTask(int taskId);
	Task UncompletedTask(int taskId);
}