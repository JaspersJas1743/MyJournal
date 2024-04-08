namespace MyJournal.Core.SubEntities;

public sealed class DayOfWeek : ISubEntity
{
	public int Id { get; init; }
	public string Name { get; init; }
}