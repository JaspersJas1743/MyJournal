namespace MyJournal.Core.SubEntities;

public sealed class SubjectTeacher : ISubEntity
{
	public int Id { get; init; }
	public string Name { get; init; } = null!;
	public string Surname { get; init; } = null!;
	public string? Patronymic { get; init; }
	public string FullName => $"{Surname} {Name} {Patronymic}";
}

public abstract class Subject : ISubEntity
{
	public int Id { get; init; }
	public string? Name { get; init; }
	public SubjectTeacher? Teacher { get; init; }
}