namespace MyJournal.Core.Utilities.Constants.Controllers;

public static class AssessmentControllerMethods
{
	public const string GetAssessments = "assessments/me/get";
	public const string GetAssessmentsForWard = "assessments/ward/get";
	public const string GetPossibleAssessments = "assessments/possible/get";
	public const string GetCommentsForTruancy = "assessments/truancy/comments/get";
	public const string Create = "assessments/create";
	public const string SetAttendance = "assessments/attendance/set";
	public const string Change = "assessments/change";
	public const string Delete = "assessments/delete";

	public static string Get(int assessmentId) => $"assessments/{assessmentId}/get";
	public static string GetAssessmentsById(int studentId) => $"assessments/student/{studentId}/get";
	public static string GetCommentsForAssessments(int assessmentId) => $"assessments/{assessmentId}/comments/get";
}