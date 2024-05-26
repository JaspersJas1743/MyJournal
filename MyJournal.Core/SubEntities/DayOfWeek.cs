namespace MyJournal.Core.SubEntities;

public sealed class DayOfWeek(int id, string name) : ISubEntity
{
	public int Id { get; init; } = id;
	public string Name { get; init; } = name;
}