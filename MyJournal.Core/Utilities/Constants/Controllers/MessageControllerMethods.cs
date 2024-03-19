namespace MyJournal.Core.Utilities.Constants.Controllers;

internal static class MessageControllerMethods
{
	internal const string GetMessages = "message/get";
	internal const string SendMessage = "message/send";

	internal static string GetMessageById(int messageId) => $"message/{messageId}/get";
}