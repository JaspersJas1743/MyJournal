namespace MyJournal.Core.Utilities.Constants;

public static class AccountControllerMethods
{
	public const string VerifyRegistrationCode = "account/registration-code/verify";
	public const string SignInWithCredentials = "account/sign-in/credentials";
	public const string SignInWithToken = "account/sign-in/token";
	public const string SignUp = "account/sign-up";
	public const string GetEmailOwner = "account/restoring-access/email/user/id/get";
	public const string GetPhoneOwner = "account/restoring-access/phone/user/id/get";
	public const string SignOutThis = "account/sign-out/this";
	public const string SignOutAll = "account/sign-out/all";
	public const string SignOutOthers = "account/sign-out/others";
	public const string GetSessions = "account/user/sessions/get";

	public static string GetGoogleAuthenticator(int userId) => $"account/sign-up/user/{userId}/code/get";
	public static string VerifyGoogleAuthenticator(int userId) => $"account/user/{userId}/code/verify";
	public static string SetEmail(int userId) => $"account/sign-up/user/{userId}/email/set";
	public static string SetPhone(int userId) => $"account/sign-up/user/{userId}/phone/set";
	public static string ResetPassword(int userId) => $"account/restoring-access/user/{userId}/password/reset";
}