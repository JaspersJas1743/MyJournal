namespace MyJournal.Core.Chats;

public sealed class LastMessage
{
	public string? Content { get; init; }
	public bool IsFile { get; init; }
	public DateTime CreatedAt { get; init; }
	public bool FromMe { get; init; }
	public bool IsRead { get; init; }
}

public sealed class Chat
{
	public int Id { get; init; }
	public string? ChatName { get; init; }
	public string? ChatPhoto { get; init; }
	public LastMessage? LastMessage { get; init; }
	public int CountOfUnreadMessages { get; init; }
}