namespace MyJournal.Core.Utilities.Constants.Hubs;

internal static class UserHubMethods
{
	internal const string HubEndpoint = "https://my-journal.ru/hub/user";
	internal const string SetOnline = nameof(SetOnline);
	internal const string SetOffline = nameof(SetOffline);
	internal const string UpdatedProfilePhoto = nameof(UpdatedProfilePhoto);
	internal const string DeletedProfilePhoto = nameof(DeletedProfilePhoto);
	internal const string SignIn = nameof(SignIn);
	internal const string SignOut = nameof(SignOut);
	internal const string JoinedInChat = nameof(JoinedInChat);
	internal const string SetPhone = nameof(SetPhone);
	internal const string SetEmail = nameof(SetEmail);
	internal const string SendMessage = nameof(SendMessage);
}