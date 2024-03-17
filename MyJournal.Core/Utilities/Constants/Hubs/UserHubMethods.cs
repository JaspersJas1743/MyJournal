namespace MyJournal.Core.Utilities.Constants.Hubs;

public static class UserHubMethods
{
	public const string HubEndpoint = "https://localhost:7267/hub/User";
	public const string SetOnline = nameof(SetOnline);
	public const string SetOffline = nameof(SetOffline);
	public const string UpdatedProfilePhoto = nameof(UpdatedProfilePhoto);
	public const string DeletedProfilePhoto = nameof(DeletedProfilePhoto);
	public const string SignIn = nameof(SignIn);
	public const string SignOut = nameof(SignOut);
	public const string JoinedInChat = nameof(JoinedInChat);
	public const string SetPhone = nameof(SetPhone);
	public const string SetEmail = nameof(SetEmail);
}