namespace MyJournal.Core;

public sealed class LastMessage(string? content, bool isFile, DateTime createdAt, bool fromMe, bool isRead)
{
	public string? Content { get; } = content;
	public bool IsFile { get; } = isFile;
	public DateTime CreatedAt { get; } = createdAt;
	public bool FromMe { get; } = fromMe;
	public bool IsRead { get; } = isRead;
}

public sealed class Chat(int id, string chatName, string chatPhoto, LastMessage? lastMessage, int countOfUnreadMessages)
{
	public int Id { get; } = id;
	public string ChatName { get; } = chatName;
	public string ChatPhoto { get; } = chatPhoto;
	public LastMessage? LastMessage { get; } = lastMessage;
	public int CountOfUnreadMessages { get; } = countOfUnreadMessages;
}