namespace MyJournal.API.Assets.Hubs;

public interface ITeacherHub
{
	Task CreatedTask(int taskId);
}