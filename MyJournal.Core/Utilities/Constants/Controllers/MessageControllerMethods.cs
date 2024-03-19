namespace MyJournal.Core.Utilities.Constants.Controllers;

public static class MessageControllerMethods
{
	public const string GetMessages = "message/get";
	public const string SendMessage = "message/send";

	public static string GetMessageById(int messageId) => $"message/{messageId}/get";
}