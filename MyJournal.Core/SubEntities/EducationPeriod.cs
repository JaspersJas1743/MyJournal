namespace MyJournal.Core.SubEntities;

public sealed class EducationPeriod : ISubEntity
{
	public int Id { get; init; }
	public string Name { get; init; }
	public DateOnly StartDate { get; init; }
	public DateOnly EndDate { get; init; }
}