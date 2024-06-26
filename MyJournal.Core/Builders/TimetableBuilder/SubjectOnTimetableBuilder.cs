using MyJournal.Core.SubEntities;

namespace MyJournal.Core.Builders.TimetableBuilder;

internal sealed class SubjectOnTimetableBuilder : BaseSubjectOnTimetableBuilder
{
	private SubjectOnTimetableBuilder() { }

	private SubjectOnTimetableBuilder(BaseSubjectOnTimetable subject)
	{
		Number = subject.Number;
		SubjectId = subject.Id;
		StartTime = subject.Start;
		EndTime = subject.End;
	}

	public override BaseSubjectOnTimetableBuilder WithNumber(int number)
	{
		Number = number;
		return this;
	}

	public override BaseSubjectOnTimetableBuilder WithSubject(int subjectId)
	{
		SubjectId = subjectId;
		return this;
	}

	public override BaseSubjectOnTimetableBuilder WithStartTime(TimeSpan time)
	{
		StartTime = time;
		return this;
	}

	public override BaseSubjectOnTimetableBuilder WithEndTime(TimeSpan time)
	{
		EndTime = time;
		return this;
	}

	internal static BaseSubjectOnTimetableBuilder Create()
		=> new SubjectOnTimetableBuilder();

	internal static BaseSubjectOnTimetableBuilder Create(SubjectOnTimetableByDay subject)
		=> new SubjectOnTimetableBuilder(subject: subject);
}