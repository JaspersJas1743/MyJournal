namespace MyJournal.Core.Builders.TimetableBuilder;

internal sealed class TimetableForDayBuilder : BaseTimetableForDayBuilder
{
	private TimetableForDayBuilder() { }

	public override BaseSubjectOnTimetableBuilder AddSubject()
	{
		BaseSubjectOnTimetableBuilder builder = SubjectOnTimetableBuilder.Create();
		Subjects.Add(item: builder);
		return builder;
	}

	internal static BaseTimetableForDayBuilder Create()
		=> new TimetableForDayBuilder();
}