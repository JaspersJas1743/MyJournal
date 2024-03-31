namespace MyJournal.Core.Utilities.Constants.Controllers;

internal static class UserControllerMethods
{
	internal const string GetInformation = "user/profile/info/me";
	internal const string SetOffline = "user/profile/activity/offline";
	internal const string SetOnline = "user/profile/activity/online";
	internal const string UploadProfilePhoto = "user/profile/photo/upload";
	internal const string DeleteProfilePhoto = "user/profile/photo/delete";
	internal const string ChangeEmail = "user/profile/security/email/change";
	internal const string ChangePhone = "user/profile/security/phone/change";
	internal const string VerifyGoogleAuthenticator = "user/profile/security/code/verify";
	internal const string ChangePassword = "user/profile/security/password/change";

	internal static string GetInformationAbout(int userId) => $"user/profile/info/{userId}";
}