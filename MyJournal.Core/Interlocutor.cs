namespace MyJournal.Core;

public sealed class Interlocutor(int userId, string photo, string name)
{
	public int UserId { get; } = userId;
	public string Photo { get; } = photo;
	public string Name { get; } = name;
}