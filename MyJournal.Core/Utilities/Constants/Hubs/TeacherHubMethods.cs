namespace MyJournal.Core.Utilities.Constants.Hubs;

internal static class TeacherHubMethods
{
	internal const string HubEndpoint = "https://localhost:7267/hub/teacher";
	internal const string CreatedTask = nameof(CreatedTask);
	internal const string StudentCompletedTask = nameof(StudentCompletedTask);
	internal const string StudentUncompletedTask = nameof(StudentUncompletedTask);
	internal const string CreatedAssessment = nameof(CreatedAssessment);
	internal const string ChangedAssessment = nameof(ChangedAssessment);
	internal const string DeletedAssessment = nameof(DeletedAssessment);
	internal const string ChangedTimetable = nameof(ChangedTimetable);
}