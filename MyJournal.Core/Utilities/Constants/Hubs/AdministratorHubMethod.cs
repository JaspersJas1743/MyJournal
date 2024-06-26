namespace MyJournal.Core.Utilities.Constants.Hubs;

internal static class AdministratorHubMethod
{
	internal const string HubEndpoint = "https://my-journal.ru/hub/administrator";
	internal const string StudentCompletedTask = nameof(StudentCompletedTask);
	internal const string StudentUncompletedTask = nameof(StudentUncompletedTask);
	internal const string CreatedTaskToStudents = nameof(CreatedTaskToStudents);
	internal const string CreatedFinalAssessmentToStudent = nameof(CreatedFinalAssessmentToStudent);
	internal const string CreatedAssessmentToStudent = nameof(CreatedAssessmentToStudent);
	internal const string ChangedAssessmentToStudent = nameof(ChangedAssessmentToStudent);
	internal const string DeletedAssessmentToStudent = nameof(DeletedAssessmentToStudent);
	internal const string ChangedTimetable = nameof(ChangedTimetable);
}