namespace MyJournal.Core.Utilities.Constants.Controllers;

internal static class TaskControllerMethods
{
	internal enum CompletionStatus
	{
		Completed,
		Uncompleted
	}

	internal const string GetAssignedTasks = "task/assigned/get";
	internal const string GetAllAssignedTasks = "task/assigned/get/all";
	internal const string GetAssignedTasksForChildren = "task/assigned/children/get";
	internal const string GetAllAssignedTasksForChildren = "task/assigned/children/get/all";
	internal const string GetCreatedTasks = "task/created/get";
	internal const string GetAllCreatedTasks = "task/created/get/all";
	internal const string CreateTask = "task/create";

	internal static string ChangeCompletionStatusForTask(int taskId, CompletionStatus completionStatus)
		=> $"task/{taskId}/completion-status/change/{completionStatus}";
}