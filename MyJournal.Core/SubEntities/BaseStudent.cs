namespace MyJournal.Core.SubEntities;

public abstract class BaseStudent : ISubEntity
{
	protected BaseStudent(
		int id,
		string surname,
		string name,
		string? patronymic
	)
	{
		Id = id;
		Surname = surname;
		Name = name;
		Patronymic = patronymic;
	}

	public int Id { get; init; }
	public string Surname { get; init; }
	public string Name { get; init; }
	public string? Patronymic { get; init; }
}