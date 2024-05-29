namespace MyJournal.Core.Utilities.Constants.Controllers;

public static class LessonControllerMethods
{
	public const string GetStudyingSubjects = "subjects/studying/get";
	public const string GetTaughtSubjects = "subjects/taught/get";
	public const string GetSubjectsStudiedByWard = "subjects/children/studying/get";

	public static string GetSubjectsStudiedInClass(int classId) => $"subjects/class/{classId}/taught/get";
	public static string GetSubjectsForClass(int classId) => $"subjects/class/{classId}/all/get";
}