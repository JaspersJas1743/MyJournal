namespace MyJournal.Core.SubEntities;

public sealed class SubjectTeacher : ISubEntity
{
	public int Id { get; init; }
	public string Name { get; init; }
	public string Surname { get; init; }
	public string? Patronymic { get; init; }
}

public sealed class StudyingSubject : ISubEntity
{
	public int Id { get; init; }
	public string Name { get; init; }
	public SubjectTeacher Teacher { get; init; }
}