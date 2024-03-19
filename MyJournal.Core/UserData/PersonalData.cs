namespace MyJournal.Core.UserData;

public sealed class PersonalData(string surname, string name, string? patronymic)
{
	public string Surname { get; init; } = surname;
	public string Name { get; init; } = name;
	public string? Patronymic { get; init; } = patronymic;
}