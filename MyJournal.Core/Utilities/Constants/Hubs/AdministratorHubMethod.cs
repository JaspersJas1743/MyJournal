namespace MyJournal.Core.Utilities.Constants.Hubs;

internal static class AdministratorHubMethod
{
	internal const string HubEndpoint = "https://localhost:7267/hub/administrator";
	internal const string StudentCompletedTask = nameof(StudentCompletedTask);
	internal const string StudentUncompletedTask = nameof(StudentUncompletedTask);
	internal const string CreatedTaskToStudents = nameof(CreatedTaskToStudents);
}