namespace MyJournal.Core.SubEntities;

public sealed class TaughtClass : ISubEntity
{
	public int Id { get; init; }
	public string Name { get; init; }
}

public sealed class TaughtSubject : ISubEntity
{
	public int Id { get; init; }
	public string Name { get; init; }
	public TaughtClass Class { get; init; }
}