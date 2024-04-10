namespace MyJournal.Core.Builders.TimetableBuilder;

public abstract class BaseSubjectOnTimetableBuilder
{
	internal int Number { get; set; } = -1;
	internal int SubjectId { get; set; } = -1;
	internal TimeSpan StartTime { get; set; } = TimeSpan.Zero;
	internal TimeSpan EndTime { get; set; } = TimeSpan.Zero;

	public abstract BaseSubjectOnTimetableBuilder WithNumber(int number);
	public abstract BaseSubjectOnTimetableBuilder WithSubject(int subjectId);
	public abstract BaseSubjectOnTimetableBuilder WithStartTime(TimeSpan time);
	public abstract BaseSubjectOnTimetableBuilder WithEndTime(TimeSpan time);
}