namespace MyJournal.Core.Utilities.Constants.Controllers;

public static class LessonControllerMethods
{
	public const string GetStudyingSubjects = "subjects/studying/get";
	public const string GetTaughtSubjects = "subjects/taught/get";
	public const string GetSubjectsStudiedByWard = "subjects/children/studying/get";

	public static string GetStudyingSubjectsByPeriod(string period) => $"subjects/studying/period/{period}/get";
	public static string GetTaughtSubjectsByPeriod(string period) => $"subjects/taught/period/{period}/get";
	public static string GetSubjectsStudiedByWardByPeriod(string period) => $"subjects/children/studying/period/{period}/get";
	public static string GetSubjectsStudiedInClassByPeriod(string period, int classId) => $"class/{classId:int}/period/{period}/get";
	public static string GetSubjectsStudiedInClass(int classId) => $"subjects/class/{classId}/get";
}