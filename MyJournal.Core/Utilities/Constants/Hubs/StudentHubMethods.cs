namespace MyJournal.Core.Utilities.Constants.Hubs;

internal static class StudentHubMethods
{
	internal const string HubEndpoint = "https://localhost:7267/hub/student";
	internal const string CompletedTask = nameof(CompletedTask);
	internal const string UncompletedTask = nameof(UncompletedTask);
	internal const string TeacherCreatedTask = nameof(TeacherCreatedTask);
	internal const string TeacherCreatedAssessment = nameof(TeacherCreatedAssessment);
	internal const string TeacherChangedAssessment = nameof(TeacherChangedAssessment);
	internal const string TeacherDeletedAssessment = nameof(TeacherDeletedAssessment);
	internal const string ChangedTimetable = nameof(ChangedTimetable);
}