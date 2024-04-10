using MyJournal.Core.SubEntities;

namespace MyJournal.Core.Builders.TimetableBuilder;

internal sealed class TimetableForDayBuilder : BaseTimetableForDayBuilder
{
	private TimetableForDayBuilder() { }

	private TimetableForDayBuilder(IEnumerable<BaseSubjectOnTimetableBuilder> subjects)
		=> Subjects.AddRange(collection: subjects);

	public override BaseSubjectOnTimetableBuilder AddSubject()
	{
		BaseSubjectOnTimetableBuilder builder = SubjectOnTimetableBuilder.Create();
		Subjects.Add(item: builder);
		return builder;
	}

	public override TimetableForDayBuilder RemoveSubject(BaseSubjectOnTimetableBuilder item)
	{
		Subjects.Remove(item: item);
		return this;
	}

	internal static BaseTimetableForDayBuilder Create()
		=> new TimetableForDayBuilder();

	internal static BaseTimetableForDayBuilder Create(IEnumerable<SubjectInClassOnTimetable> subjects)
	{
		return new TimetableForDayBuilder(subjects: subjects.Select(selector:
			s => SubjectOnTimetableBuilder.Create(subject: s.Subject)
		));
	}
}