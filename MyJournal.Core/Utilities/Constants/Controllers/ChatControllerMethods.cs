namespace MyJournal.Core.Utilities.Constants.Controllers;

internal static class ChatControllerMethods
{
	internal const string GetChats = "chat/get";
	internal const string GetInterlocutors = "chat/interlocutors/get";
	internal const string GetIntendedInterlocutors = "chat/intended-interlocutors/get";
	internal const string CreateSingleChat = "chat/single/create";
	internal const string CreateMultiChat = "chat/multi/create";

	internal static string GetChat(int chatId) => $"chat/{chatId}/get";
	internal static string ReadChat(int chatId) => $"chat/{chatId}/read";
}