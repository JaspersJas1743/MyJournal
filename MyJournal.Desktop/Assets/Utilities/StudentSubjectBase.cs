using MyJournal.Core.SubEntities;

namespace MyJournal.Desktop.Assets.Utilities;

public abstract class StudentSubjectBase
{
	public int Id { get; init; }
	public string? Name { get; init; }
	public SubjectTeacher? Teacher { get; init; }
}