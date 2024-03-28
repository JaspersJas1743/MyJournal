namespace MyJournal.Core.Utilities.Constants.Controllers;

public static class AdministratorControllerMethods
{
	public static string GetEducationPeriods(int classId) => $"administrator/periods/education/class/{classId}/get";
}