namespace MyJournal.Core.Utilities.Constants.Controllers;

public static class UserControllerMethods
{
	public const string GetInformation = "user/profile/info/me";
	public const string SetOffline = "user/profile/activity/offline";
	public const string SetOnline = "user/profile/activity/online";
	public const string UploadProfilePhoto = "user/profile/photo/upload";
	public const string DownloadProfilePhoto = "user/profile/photo/download";
	public const string DeleteProfilePhoto = "user/profile/photo/delete";
	public const string ChangeEmail = "user/profile/security/email/change";
	public const string ChangePhone = "user/profile/security/phone/change";
	public const string VerifyGoogleAuthenticator = "user/profile/security/code/verify";
	public const string ChangePassword = "user/profile/security/password/change";

	public static string GetInformationAbout(int userId) => $"user/profile/info/{userId}";
}