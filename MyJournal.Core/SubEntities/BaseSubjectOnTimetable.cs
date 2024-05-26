namespace MyJournal.Core.SubEntities;

public abstract class BaseSubjectOnTimetable(int id, int number, TimeSpan start, TimeSpan end) : ISubEntity
{
	public int Id { get; init; } = id;
	public int Number { get; init; } = number;
	public TimeSpan Start { get; init; } = start;
	public TimeSpan End { get; init; } = end;
}

public sealed class SubjectOnTimetableByDay(int id, int number, TimeSpan start, TimeSpan end, string name)
	: BaseSubjectOnTimetable(id: id, number: number, start: start, end: end)
{
	public string Name { get; init; } = name;
}

public sealed class SubjectOnTimetable(int id, int number, TimeSpan start, TimeSpan end, string className, DateOnly date, string name)
	: BaseSubjectOnTimetable(id: id, number: number, start: start, end: end)
{
	public string Name { get; init; } = name;
	public string ClassName { get; init; } = className;
	public DateOnly Date { get; init; } = date;
}

public sealed class SubjectInClassOnTimetable(SubjectOnTimetableByDay subject, BreakAfterSubject @break)
{
	public SubjectOnTimetableByDay Subject { get; set; } = subject;
	public BreakAfterSubject? Break { get; set; } = @break;
}