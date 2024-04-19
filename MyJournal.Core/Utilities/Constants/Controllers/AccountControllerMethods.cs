namespace MyJournal.Core.Utilities.Constants.Controllers;

internal static class AccountControllerMethods
{
	internal const string VerifyRegistrationCode = "account/registration-code/verify";
	internal const string VerifyLogin = "account/login/verify";
	internal const string SignInWithCredentials = "account/sign-in/credentials";
	internal const string SignInWithToken = "account/sign-in/token";
	internal const string SignUp = "account/sign-up";
	internal const string GetEmailOwner = "account/restoring-access/email/user/id/get";
	internal const string GetPhoneOwner = "account/restoring-access/phone/user/id/get";
	internal const string SignOutThis = "account/sign-out/this";
	internal const string SignOutAll = "account/sign-out/all";
	internal const string SignOutOthers = "account/sign-out/others";
	internal const string GetSessions = "account/user/sessions/get";

	internal static string GetSession(int sessionId) => $"account/user/sessions/{sessionId}/get";
	internal static string SignOutSession(int sessionId) => $"account/sign-out/{sessionId}";
	internal static string GetGoogleAuthenticator(int userId) => $"account/sign-up/user/{userId}/code/get";
	internal static string VerifyGoogleAuthenticator(int userId) => $"account/user/{userId}/code/verify";
	internal static string SetEmail(int userId) => $"account/sign-up/user/{userId}/email/set";
	internal static string SetPhone(int userId) => $"account/sign-up/user/{userId}/phone/set";
	internal static string ResetPassword(int userId) => $"account/restoring-access/user/{userId}/password/reset";
}