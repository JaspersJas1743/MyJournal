using System.Collections;

namespace MyJournal.Core.Builders.TimetableBuilder;

public abstract class BaseTimetableForDayBuilder : IEnumerable<BaseSubjectOnTimetableBuilder>
{
	internal List<BaseSubjectOnTimetableBuilder> Subjects { get; } = new List<BaseSubjectOnTimetableBuilder>();

	public abstract BaseSubjectOnTimetableBuilder AddSubject();
	public abstract BaseTimetableForDayBuilder RemoveSubject(BaseSubjectOnTimetableBuilder item);

	public IEnumerator<BaseSubjectOnTimetableBuilder> GetEnumerator()
		=> Subjects.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() =>
		GetEnumerator();
}