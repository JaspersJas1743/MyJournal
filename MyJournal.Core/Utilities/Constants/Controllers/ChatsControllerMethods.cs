namespace MyJournal.Core.Utilities.Constants.Controllers;

public static class ChatsControllerMethods
{
	public const string GetChats = "chat/get";
	public const string GetInterlocutors = "chat/interlocutors/get";
	public const string CreateSingleChat = "chat/single/create";
	public const string CreateMultiChat = "chat/multi/create";

	public static string GetChat(int chatId) => $"chat/{chatId}/get";
}