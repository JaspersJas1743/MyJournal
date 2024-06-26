namespace MyJournal.Core.Utilities.Constants.Hubs;

internal static class ParentHubMethods
{
	internal const string HubEndpoint = "https://my-journal.ru/hub/parent";
	internal const string WardCompletedTask = nameof(WardCompletedTask);
	internal const string WardUncompletedTask = nameof(WardUncompletedTask);
	internal const string CreatedTaskToWard = nameof(CreatedTaskToWard);
	internal const string CreatedFinalAssessmentToWard = nameof(CreatedFinalAssessmentToWard);
	internal const string CreatedAssessmentToWard = nameof(CreatedAssessmentToWard);
	internal const string ChangedAssessmentToWard = nameof(ChangedAssessmentToWard);
	internal const string DeletedAssessmentToWard = nameof(DeletedAssessmentToWard);
	internal const string ChangedTimetable = nameof(ChangedTimetable);
}