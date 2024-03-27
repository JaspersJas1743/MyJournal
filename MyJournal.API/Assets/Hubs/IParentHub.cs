namespace MyJournal.API.Assets.Hubs;

public interface IParentHub
{
	Task WardCompletedTask(int taskId);
	Task WardUncompletedTask(int taskId);
	Task CreatedTaskToWard(int taskId);
}