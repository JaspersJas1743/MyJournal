namespace MyJournal.Core.Builders.TimetableBuilder;

public abstract class BaseTimetableForDayBuilder
{
	internal List<BaseSubjectOnTimetableBuilder> Subjects { get; } = new List<BaseSubjectOnTimetableBuilder>();

	public abstract BaseSubjectOnTimetableBuilder AddSubject();
}