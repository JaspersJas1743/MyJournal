namespace MyJournal.Core.Utilities.Constants.Controllers;

public static class ClassControllerMethods
{
	public const string GetClasses = "class/all/get";

	public static string GetEducationPeriodsByClass(int classId) => $"class/{classId}/periods/get";
}