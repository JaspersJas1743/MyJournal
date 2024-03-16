namespace MyJournal.Core;

public enum ActivityStatus
{
	Online,
	Offline
}

public sealed class UserInformation(int id, string surname, string name, string? patronymic, string? photo, ActivityStatus activity, DateTime? onlineAt)
{
	public int Id { get; } = id;
	public string Surname { get; } = surname;
	public string Name { get; } = name;
	public string? Patronymic { get; } = patronymic;
	public string? Photo { get; } = photo;
	public ActivityStatus Activity { get; } = activity;
	public DateTime? OnlineAt { get; } = onlineAt;
}