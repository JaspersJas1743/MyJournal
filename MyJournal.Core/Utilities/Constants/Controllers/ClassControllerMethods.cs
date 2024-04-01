namespace MyJournal.Core.Utilities.Constants.Controllers;

public static class ClassControllerMethods
{
	public const string GetClasses = "class/all/get";

	public static string GetStudentsFromClass(int classId) => $"class/{classId}/students/get";
}