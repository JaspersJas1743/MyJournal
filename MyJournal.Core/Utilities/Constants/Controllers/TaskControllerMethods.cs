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

	internal static string GetAssignedTaskById(int taskId) => $"task/assigned/get/{taskId}";
	internal static string GetCreatedTaskById(int taskId) => $"task/created/get/{taskId}";
	internal static string GetAssignedByClassTasks(int classId) => $"task/assigned/class/{classId}/get";
	internal static string GetAllAssignedByClassTasks(int classId) => $"task/assigned/class/{classId}/get/all";
	internal static string ChangeCompletionStatusForTask(int taskId, CompletionStatus completionStatus)
		=> $"task/{taskId}/completion-status/change/{completionStatus}";
}