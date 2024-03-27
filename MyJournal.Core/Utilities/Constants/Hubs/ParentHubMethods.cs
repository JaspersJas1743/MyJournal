namespace MyJournal.Core.Utilities.Constants.Hubs;

internal static class ParentHubMethods
{
	internal const string HubEndpoint = "https://localhost:7267/hub/parent";
	internal const string WardCompletedTask = nameof(WardCompletedTask);
	internal const string WardUncompletedTask = nameof(WardUncompletedTask);
	internal const string CreatedTaskToWard = nameof(CreatedTaskToWard);
}