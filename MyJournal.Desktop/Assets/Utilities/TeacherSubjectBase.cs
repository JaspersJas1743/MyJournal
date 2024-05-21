namespace MyJournal.Desktop.Assets.Utilities;

public abstract class TeacherSubjectBase
{
	public int Id { get; init; }
	public string? Name { get; init; }
	public int? ClassId { get; protected set; }
	public string? ClassName { get; protected set; }
}